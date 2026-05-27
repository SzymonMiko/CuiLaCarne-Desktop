using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuiLaCarne.Models.DTOS
{
    public class SyncIngredientResponse
    {
        public string Token { get; set; } = "";

        public string Name { get; set; } = "";

        public string NamePl { get; set; } = "";

        public string NameEn { get; set; } = "";

        public string DisplayName =>
            !string.IsNullOrWhiteSpace(NamePl)
                ? NamePl
                : !string.IsNullOrWhiteSpace(NameEn)
                    ? NameEn
                    : !string.IsNullOrWhiteSpace(Name)
                        ? Name
                        : Token;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
