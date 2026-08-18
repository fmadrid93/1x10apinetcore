using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Result
{
    public class ResultElement<TElement>
    {
        public TElement Data { get; set; }

        public List<ResultError> Errors { get; set; }
        public bool HasError
        {
            get
            {
                return !(Errors?.Count == 0);
            }
        }

        public ResultElement()
        {
            Errors = new List<ResultError>();
        }
    }
}
