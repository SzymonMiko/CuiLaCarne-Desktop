using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS;
public class DictionaryWrapper<T>
{
    public List<T> Item { get; set; } = [];
}