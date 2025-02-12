using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Components;
using Terminal.Core.Domains;
using Terminal.Core.Enums;
using Terminal.Core.Models;

namespace Terminal.Pages.Options
{
  public partial class CoveredDirection
  {
    public virtual OptionPageComponent OptionView { get; set; }

    /// <summary>
    /// Setup views and adapters
    /// </summary>
    /// <param name="setup"></param>
    /// <returns></returns>
    protected override async Task OnAfterRenderAsync(bool setup)
    {
      if (setup)
      {
        OptionView.Instrument = new InstrumentModel
        {
          Name = "SPY",
          TimeFrame = TimeSpan.FromMinutes(5)
        };

        await OptionView.OnLoad(OnData);
      }

      await base.OnAfterRenderAsync(setup);
    }

    /// <summary>
    /// Process tick data
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    protected async Task OnData(PointModel point)
    {
      var adapter = OptionView.View.Adapters["Sim"];
      var account = adapter.Account;

      await OptionView.OnUpdate(point, 30, async options =>
      {
        if (account.Orders.Count is 0 && account.Positions.Count is 0)
        {
          var orders = GetOrders(adapter, point, OptionSideEnum.Call, options);
          var orderResponse = await adapter.CreateOrders([.. orders]);
        }
      });
    }

    /// <summary>
    /// Create PMCC strategy
    /// </summary>
    /// <param name="adapter"></param>
    /// <param name="point"></param>
    /// <param name="side"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    protected static IList<OrderModel> GetOrders(IGateway adapter, PointModel point, OptionSideEnum side, IList<InstrumentModel> options)
    {
      var account = adapter.Account;
      var sideOptions = options.Where(o => Equals(o.Derivative.Side, side));
      var minDate = options.First().Derivative.ExpirationDate;
      var maxDate = options.Last().Derivative.ExpirationDate;
      var longOptions = sideOptions.Where(o => o.Derivative.ExpirationDate >= maxDate);
      var shortOptions = sideOptions.Where(o => o.Derivative.ExpirationDate <= minDate);
      var order = new OrderModel
      {
        Type = OrderTypeEnum.Market,
        Orders =
        [
          new OrderModel
          {
            Volume = 2,
            Side = OrderSideEnum.Long,
            Instruction = InstructionEnum.Side,
            Transaction = new ()
          },
          new OrderModel
          {
            Volume = 1,
            Side = OrderSideEnum.Short,
            Instruction = InstructionEnum.Side,
            Transaction = new ()
          }
        ]
      };

      switch (side)
      {
        case OptionSideEnum.Put:

          var put = order.Orders[0].Transaction.Instrument = longOptions
            .Where(o => o.Derivative.Strike > point.Last)
            .FirstOrDefault();

          order.Orders[1].Transaction.Instrument = shortOptions
            .Where(o => o.Derivative.Strike < point.Last)
            .LastOrDefault();

          break;

        case OptionSideEnum.Call:

          var call = order.Orders[0].Transaction.Instrument = longOptions
            .Where(o => o.Derivative.Strike < point.Last)
            .LastOrDefault();

          order.Orders[1].Transaction.Instrument = shortOptions
            .Where(o => o.Derivative.Strike > point.Last)
            .FirstOrDefault();

          break;
      }

      return [order];
    }
  }
}
