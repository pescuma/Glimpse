﻿using System;

namespace Glimpse.Core.Message
{
    /// <summary>
    /// Message that captures trace events.
    /// </summary>
    public class TraceMessage : ITraceMessage, ITimelineMessage
    {
        /// <summary>
        /// Gets or sets the category the message is in.
        /// </summary>
        /// <value>The category.</value>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        /// <value>The message.</value>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the time from the request start.
        /// </summary>
        /// <value>From first.</value>
        public TimeSpan FromFirst { get; set; }

        /// <summary>
        /// Gets or sets the time from the last trace event.
        /// </summary>
        /// <value>From last.</value>
        public TimeSpan FromLast { get; set; }

        /// <summary>
        /// Gets or sets the indent level of the message.
        /// </summary>
        /// <value>The indent level.</value>
        public int IndentLevel { get; set; }

        public Guid Id { get; private set; }
        public TimeSpan Offset { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; set; }
        public string EventName { get; set; }
        public TimelineCategoryItem EventCategory { get; set; }
        public string EventSubText { get; set; }

        public TraceMessage()
        {
            Id = Guid.NewGuid();
        }
    }
}
