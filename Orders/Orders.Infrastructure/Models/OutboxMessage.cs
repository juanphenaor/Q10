using System;
using System.Collections.Generic;
using System.Text;

namespace Orders.Infrastructure.Models
{
    public sealed class OutboxMessage
    {
        public Guid Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Payload { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public int Attempts { get; set; }
    }
}
