using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Core.Enums;

namespace Terminal.Core.Models
{
  public class PositionModel : ICloneable
  {
    /// <summary>
    /// Actual PnL measured in account's currency
    /// </summary>
    public virtual decimal? GainLoss { get; set; }

    /// <summary>
    /// Min possible PnL in account's currency
    /// </summary>
    public virtual decimal? GainLossMin { get; set; }

    /// <summary>
    /// Max possible PnL in account's currency
    /// </summary>
    public virtual decimal? GainLossMax { get; set; }

    /// <summary>
    /// Actual PnL in points
    /// </summary>
    public virtual decimal? GainLossPoints { get; set; }

    /// <summary>
    /// Min possible PnL in points
    /// </summary>
    public virtual decimal? GainLossPointsMin { get; set; }

    /// <summary>
    /// Max possible PnL in points
    /// </summary>
    public virtual decimal? GainLossPointsMax { get; set; }

    /// <summary>
    /// Aggregated order
    /// </summary>
    public virtual OrderModel Order { get; set; }

    /// <summary>
    /// Related orders
    /// </summary>
    public virtual IList<OrderModel> Orders { get; set; }

    /// <summary>
    /// Close price estimate
    /// </summary>
    public virtual decimal? ClosePriceEstimate
    {
      get
      {
        var point = Order.Transaction.Instrument.Points.LastOrDefault();

        if (point is not null)
        {
          switch (Order.Side)
          {
            case OrderSideEnum.Buy: return point.Bid;
            case OrderSideEnum.Sell: return point.Ask;
          }
        }

        return null;
      }
    }

    /// <summary>
    /// Estimated PnL in account's currency
    /// </summary>
    public virtual decimal? GainLossEstimate => GetGainLossEstimate(Order.Transaction.Price);

    /// <summary>
    /// Estimated PnL in points
    /// </summary>
    public virtual decimal? GainLossPointsEstimate => GetGainLossPointsEstimate(Order.Transaction.Price);

    /// <summary>
    /// Cummulative estimated PnL in account's currency for all positions in the same direction
    /// </summary>
    public virtual decimal? GainLossAverageEstimate => GetGainLossPointsEstimate();

    /// <summary>
    /// Cummulative estimated PnL in points for all positions in the same direction
    /// </summary>
    public virtual decimal? GainLossPointsAverageEstimate => GetGainLossEstimate();

    /// <summary>
    /// Estimated PnL in points
    /// </summary>
    /// <param name="price"></param>
    /// <returns></returns>
    protected virtual decimal? GetGainLossPointsEstimate(decimal? price = null)
    {
      var direction = 0;

      switch (Order.Side)
      {
        case OrderSideEnum.Buy: direction = 1; break;
        case OrderSideEnum.Sell: direction = -1; break;
      }

      var estimate = ((ClosePriceEstimate - Order.Transaction.Price) * direction) ?? 0;

      if (price is not null)
      {
        estimate = ((ClosePriceEstimate - price) * direction) ?? 0;

        GainLossPointsMin = Math.Min(GainLossPointsMin ?? estimate, estimate);
        GainLossPointsMax = Math.Max(GainLossPointsMax ?? estimate, estimate);
      }

      return estimate;
    }

    /// <summary>
    /// Estimated PnL in account's currency
    /// </summary>
    /// <param name="price"></param>
    /// <returns></returns>
    protected virtual decimal? GetGainLossEstimate(decimal? price = null)
    {
      var action = Order.Transaction;
      var step = action.Instrument.StepValue / action.Instrument.StepSize;
      var estimate = action.Volume * (GetGainLossPointsEstimate(price) * step - action.Instrument.Commission) ?? 0;

      if (price is not null)
      {
        GainLossMin = Math.Min(GainLossMin ?? estimate, estimate);
        GainLossMax = Math.Max(GainLossMax ?? estimate, estimate);
      }

      return estimate;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public PositionModel()
    {
      Orders = [];
    }

    /// <summary>
    /// Clone
    /// </summary>
    public virtual object Clone()
    {
      var clone = MemberwiseClone() as PositionModel;

      clone.Order = Order?.Clone() as OrderModel;

      return clone;
    }
  }
}
