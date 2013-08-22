﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common; 
using System.Reflection;
using System.Text;
using Glimpse.Ado.Message;
using Glimpse.Core.Message;

namespace Glimpse.Ado.AlternateType
{
    public static class Support
    {
        public static DbProviderFactory TryGetProviderFactory(this DbConnection connection)
        {
            // If we can pull it out quickly and easily
            var profiledConnection = connection as GlimpseDbConnection;
            if (profiledConnection != null)
            {
                return profiledConnection.InnerProviderFactory;
            }

#if (NET45)
            return DbProviderFactories.GetFactory(connection);
#else
            return connection.GetType().GetProperty("ProviderFactory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(connection, null) as DbProviderFactory;
#endif
        }

        public static DbProviderFactory TryGetProfiledProviderFactory(this DbConnection connection)
        {
            var factory = connection.TryGetProviderFactory();
            if (factory != null)
            { 
                if (!(factory is GlimpseDbProviderFactory))
                {
                    factory = factory.WrapProviderFactory(); 
                }
            }
            else
            {
                throw new NotSupportedException(string.Format(Resources.DbFactoryNotFoundInDbConnection, connection.GetType().FullName));
            }

            return factory;
        }

        public static DbProviderFactory WrapProviderFactory(this DbProviderFactory factory)
        {
            if (!(factory is GlimpseDbProviderFactory))
            { 
                var factoryType = typeof(GlimpseDbProviderFactory<>).MakeGenericType(factory.GetType());
                return (DbProviderFactory)factoryType.GetField("Instance").GetValue(null);    
            }

            return factory;
        }

        public static DataTable FindDbProviderFactoryTable()
        {
            var dbProviderFactories = typeof(DbProviderFactories);
            var providerField = dbProviderFactories.GetField("_configTable", BindingFlags.NonPublic | BindingFlags.Static) ?? dbProviderFactories.GetField("_providerTable", BindingFlags.NonPublic | BindingFlags.Static);
            var registrations = providerField.GetValue(null);
            return registrations is DataSet ? ((DataSet)registrations).Tables["DbProviderFactories"] : (DataTable)registrations;
        }

        public static object GetParameterValue(IDataParameter parameter)
        {
            if (parameter.Value == DBNull.Value)
            {
                return "NULL";
            }

            if (parameter.Value is byte[])
            {
                var builder = new StringBuilder("0x");
                foreach (var num in (byte[])parameter.Value)
                {
                    builder.Append(num.ToString("X2"));
                }

                return builder.ToString();
            }
            return parameter.Value;
        }

        public static TimeSpan LogCommandSeed(this GlimpseDbCommand command)
        {
            return command.TimerStrategy != null ? command.TimerStrategy.Start() : TimeSpan.Zero;
        }

        public static void LogCommandStart(this GlimpseDbCommand command, Guid commandId, TimeSpan timerTimeSpan)
        {
            if (command.MessageBroker != null)
            {
                IList<CommandExecutedParamater> parameters = null;
                if (command.Parameters.Count > 0)
                {
                    parameters = new List<CommandExecutedParamater>();
                    foreach (IDbDataParameter parameter in command.Parameters)
                    {
                        var parameterName = parameter.ParameterName;
                        if (!parameterName.StartsWith("@"))
                        {
                            parameterName = "@" + parameterName;
                        }

                        parameters.Add(new CommandExecutedParamater { Name = parameterName, Value = GetParameterValue(parameter), Type = parameter.DbType.ToString(), Size = parameter.Size });
                    }
                }

                command.MessageBroker.Publish(
                    new CommandExecutedMessage(command.InnerConnection.ConnectionId, commandId, command.InnerCommand.CommandText, parameters, command.InnerCommand.Transaction != null)
                    .AsTimedMessage(timerTimeSpan));
            }
        }

        public static void LogCommandEnd(this GlimpseDbCommand command, Guid commandId, TimeSpan timer, int? recordsAffected, string type)
        {
            if (command.MessageBroker != null && command.TimerStrategy != null)
            {
                command.MessageBroker.Publish(
                    new CommandDurationAndRowCountMessage(command.InnerConnection.ConnectionId, commandId, recordsAffected)
                    .AsTimedMessage(command.TimerStrategy.Stop(timer))
                    .AsTimelineMessage(command.CommandText, AdoTimelineCategory.Command, type + (recordsAffected.HasValue? "\n" + recordsAffected + " records affected" : "")));
            }
        }

        public static void LogCommandError(this GlimpseDbCommand command, Guid commandId, TimeSpan timer, Exception exception, string type)
        {
            if (command.MessageBroker != null && command.TimerStrategy != null)
            {
                command.MessageBroker.Publish(
                    new CommandErrorMessage(command.InnerConnection.ConnectionId, commandId, exception)
                    .AsTimedMessage(command.TimerStrategy.Stop(timer))
                    .AsTimelineMessage("Error: " + command.CommandText, AdoTimelineCategory.Command, type));
            }
        }
    }
}
