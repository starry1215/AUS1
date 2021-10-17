using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcerUserSensing
{
    class Result
    {
        private string _reason;
        public string Reason
        {
            get
            {
                return _reason;
            }
            set
            {
                _reason = value;
                Success = String.IsNullOrEmpty(_reason) ? true : false;
            }
        }
        public Boolean Success { get; set; }

        public Result()
        {
            Reason = null;
            Success = true;
        }
        public Result(string reason)
        {
            Reason = reason;
            Success = String.IsNullOrEmpty(reason) ? true : false;
        }
    }
}
