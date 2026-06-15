using System;
using System.Collections.Generic;

namespace Demo.Models
{
    /// <summary>
    /// Sample customer model used in rule expressions and actions.
    /// </summary>
    public class Customer : IEquatable<Customer>
    {
        /// <summary>
        /// Unique identifier for the customer.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Customer full name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Customer age in years.
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// Customer email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Whether the customer account is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether the customer is a VIP.
        /// </summary>
        public bool IsVip { get; set; }

        /// <summary>
        /// Account creation date.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Customer tags for segmentation.
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Customer orders.
        /// </summary>
        public List<Order> Orders { get; set; } = new();

        /// <summary>
        /// Compares customers by Id.
        /// </summary>
        public bool Equals(Customer? c) => Id == c?.Id;

        public override bool Equals(object? obj) => Equals(obj as Customer);
        public override int GetHashCode() => Id.GetHashCode();
    }

    /// <summary>
    /// Customer order for demo expressions.
    /// </summary>
    public class Order
    {
        public int Id { get; set; }
        public double Total { get; set; }
        public List<string> Items { get; set; } = new();
    }
}
