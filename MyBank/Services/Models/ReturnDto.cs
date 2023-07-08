using MyBank.Infrastructure.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBank.Services.Models
{
    public class ReturnDto<T>
    {
        public bool IsValid { get; set; }
        public string? Message { get; set; }
        public T? Values { get; set; }
}
}
