using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akuna.UI.Model
{
    public class Instrument
    {
        public uint InstrumentId { get; set; }

        public double BidPx { get; set; }

        public uint BidQty { get; set; }

        public double AskPx { get; set; }

        public uint AskQty { get; set; }

        public uint Volume { get; set; }

        public Instrument()
        {

        }

        public Instrument(uint instrumentId, double bidPx, uint bidQty, double askPx, uint askQty, uint vol)
        {
            InstrumentId = instrumentId;
            BidPx = bidPx;
            BidQty = bidQty;
            AskPx = askPx;
            AskQty = askQty;
            Volume = vol;
        }
    }
}
