using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Core.EnumSpace;
using Terminal.Core.ServiceSpace;
using Terminal.Core.ValidatorSpace;

namespace Terminal.Core.ModelSpace
{
  /// <summary>
  /// Generic position model
  /// </summary>
  public interface ITransactionPositionModel : ITransactionOrderModel
  {
    /// <summary>
    /// Actual PnL in account's currency
    /// </summary>
    double? GainLoss { get; set; }

    /// <summary>
    /// Min possible PnL in account's currency
    /// </summary>
    double? GainLossMin { get; set; }

    /// <summary>
    /// Max possible PnL in account's currency
    /// </summary>
    double? GainLossMax { get; set; }

    /// <summary>
    /// Actual PnL in points
    /// </summary>
    double? GainLossPoints { get; set; }

    /// <summary>
    /// Min possible PnL in points
    /// </summary>
    double? GainLossPointsMin { get; set; }

    /// <summary>
    /// Max possible PnL in points
    /// </summary>
    double? GainLossPointsMax { get; set; }

    /// <summary>
    /// Estimated PnL in account's currency
    /// </summary>
    double? GainLossEstimate { get; }

    /// <summary>
    /// Estimated PnL in points
    /// </summary>
    double? GainLossPointsEstimate { get; }

    /// <summary>
    /// Cummulative estimated PnL in account's currency for all positions in the same direction
    /// </summary>
    double? GainLossAverageEstimate { get; }

    /// <summary>
    /// Cummulative estimated PnL in points for all positions in the same direction
    /// </summary>
    double? GainLossPointsAverageEstimate { get; }

    /// <summary>
    /// Open price
    /// </summary>
    double? OpenPrice { get; set; }

    /// <summary>
    /// Close price
    /// </summary>
    double? ClosePrice { get; set; }

    /// <summary>
    /// Close price estimate
    /// </summary>
    double? ClosePriceEstimate { get; }

    /// <summary>
    /// Time stamp of when position was closed or replaced with the new one
    /// </summary>
    DateTime? CloseTime { get; set; }

    /// <summary>
    /// Sum of all open prices added to the position
    /// </summary>
    IList<ITransactionOrderModel> OpenPrices { get; set; }
  }

  /// <summary>
  /// Generic position model
  /// </summary>
  public class TransactionPositionModel : TransactionOrderModel, ITransactionPositionModel
  {
    /// <summary>
    /// Actual PnL measured in account's currency
    /// </summary>
    public virtual double? GainLoss { get; set; }

    /// <summary>
    /// Min possible PnL in account's currency
    /// </summary>
    public virtual double? GainLossMin { get; set; }

    /// <summary>
    /// Max possible PnL in account's currency
    /// </summary>
    public virtual double? GainLossMax { get; set; }

    /// <summary>
    /// Actual PnL in points
    /// </summary>
    public virtual double? GainLossPoints { get; set; }

    /// <summary>
    /// Min possible PnL in points
    /// </summary>
    public virtual double? GainLossPointsMin { get; set; }

    /// <summary>
    /// Max possible PnL in points
    /// </summary>
    public virtual double? GainLossPointsMax { get; set; }

    /// <summary>
    /// Open price
    /// </summary>
    public virtual double? OpenPrice { get; set; }

    /// <summary>
    /// Close price
    /// </summary>
    public virtual double? ClosePrice { get; set; }

    /// <summary>
    /// Time stamp of when position was closed or replaced with the new one
    /// </summary>
    public virtual DateTime? CloseTime { get; set; }

    /// <summary>
    /// Sum of all open prices added to the position
    /// </summary>
    public virtual IList<ITransactionOrderModel> OpenPrices { get; set; }

    /// <summary>
    /// Close price estimate
    /// </summary>
    public virtual double? ClosePriceEstimate
    {
      get
      {
        var point = Instrument.PointGroups.LastOrDefault();

        if (point != null)
        {
          switch (Side)
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
    public virtual double? GainLossEstimate => GetGainLossEstimate(Price);

    /// <summary>
    /// Estimated PnL in points
    /// </summary>
    public virtual double? GainLossPointsEstimate => GetGainLossPointsEstimate(Price);

    /// <summary>
    /// Cummulative estimated PnL in account's currency for all positions in the same direction
    /// </summary>
    public virtual double? GainLossAverageEstimate => GetGainLossPointsEstimate();

    /// <summary>
    /// Cummulative estimated PnL in points for all positions in the same direction
    /// </summary>
    public virtual double? GainLossPointsAverageEstimate => GetGainLossEstimate();

    /// <summary>
    /// Estimated PnL in points
    /// </summary>
    /// <param name="price"></param>
    /// <returns></returns>
    protected virtual double? GetGainLossPointsEstimate(double? price = null)
    {
      var direction = 0;

      switch (Side)
      {
        case OrderSideEnum.Buy: direction = 1; break;
        case OrderSideEnum.Sell: direction = -1; break;
      }

      var estimate = (ClosePriceEstimate - OpenPrice) * direction;

      if (price != null)
      {
        estimate = (ClosePriceEstimate - price) * direction;

        GainLossPointsMin = Math.Min(GainLossPointsMin ?? 0.0, estimate ?? 0.0);
        GainLossPointsMax = Math.Max(GainLossPointsMax ?? 0.0, estimate ?? 0.0);
      }

      return estimate;
    }

    /// <summary>
    /// Estimated PnL in account's currency
    /// </summary>
    /// <param name="price"></param>
    /// <returns></returns>
    protected virtual double? GetGainLossEstimate(double? price = null)
    {
      var instrumentErrors = InstanceService<InstrumentCollectionsValidator>
        .Instance
        .Validate(Instrument)
        .Errors;

      if (instrumentErrors.Any() == false)
      {
        var delta = Instrument.StepValue / Instrument.StepSize;
        var commission = Instrument.Commission * OpenPrices.Count * 2;
        var estimate = Size * (GetGainLossPointsEstimate(price) * delta - commission);

        if (price != null)
        {
          GainLossMin = Math.Min(GainLossMin ?? 0.0, estimate ?? 0.0);
          GainLossMax = Math.Max(GainLossMax ?? 0.0, estimate ?? 0.0);
        }

        return estimate;
      }

      InstanceService<LogService>.Instance.Log.Error("Incorrect instrument");

      return null;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public TransactionPositionModel()
    {
      OpenPrices = new List<ITransactionOrderModel>();
    }
  }
}
