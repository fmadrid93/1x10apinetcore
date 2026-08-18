using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Infrastructure;

namespace Application.Bot
{
  

    public class BotService
    {
        private readonly DBot _data = new DBot();

        public DataTable ObtenerRecinto(string celular)
        {
            return _data.ObtenerRecinto(celular);
        }
    }
}
