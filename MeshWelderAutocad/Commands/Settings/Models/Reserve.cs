using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshWelderAutocad.Commands.Settings.Models
{
    internal class Reserve
    {
        public int MinBoardCount { get; set; }
        public int MaxBoardCount { get; set; }
        public int ReserveBoardCount { get; set; }
        public Reserve(int minBoardCount, int maxBoardCount, int reserveBoardCount)
        {
            MinBoardCount = minBoardCount;
            MaxBoardCount = maxBoardCount;
            ReserveBoardCount = reserveBoardCount;
        }
        public Reserve()
        {

        }
    }
}
