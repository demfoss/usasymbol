using System;
using System.Collections.Generic;
using usasymbol.Models;
using USASymbol.Models;

namespace USASymbol.Models.ViewModels
{
    public class SymbolDetailViewModel : IStateScopedViewModel
    {
        public State State { get; set; } = new();
        public Symbol Symbol { get; set; } = new();
        public List<Symbol> RelatedSymbols { get; set; } = new();
        public List<QuizQuestion>? QuizQuestions { get; set; } = new();
        public virtual IReadOnlyList<QuickFactItem>? QuickFacts { get; set; } = new List<QuickFactItem>();

        public SymbolContent? Content { get; set; }

        public virtual string? WikidataId => Symbol?.WikidataId;
        public virtual string? Legislation => Symbol?.Legislation;
        public virtual string? Meaning => Symbol?.Meaning;

        public virtual string? Author => null;
        public virtual DateTime? DateModified => null;

        public AuthorBox AuthorBox => new AuthorBox
        {
            DateModified = DateModified,
            Author = Author
        };

        protected static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values)
                if (!string.IsNullOrWhiteSpace(v)) return v;
            return null;
        }
    }
}
