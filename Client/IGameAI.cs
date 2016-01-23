using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public interface IGameAI
    {
        void Update();
    }

    public class EmptyAI : IGameAI
    {
        public void Update()
        { }
    }
}
