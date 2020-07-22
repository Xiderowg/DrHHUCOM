using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dr_HHU.ViewModel
{
    public class StatusPresenterModel:BindableProperty
    {
        public string Status
        {
            set
            {
                UpdateProper(ref _status, value);
            }
            get
            {
                return _status;
            }
        }
        private string _status = "程序初始化完毕...";
    }
}
