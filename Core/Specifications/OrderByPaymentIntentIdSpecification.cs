using System;
using System.Linq.Expressions;
using Core.Entities.OrderAggregate;

namespace API.Specifications
{
    public class OrderByPaymentIntentIdSpecification: BaseSpecification<Order>
    {
        public OrderByPaymentIntentIdSpecification(string paymentIntentId) : base(o=>o.PaymentId == paymentIntentId)
        {
        }
    }
}