namespace USASymbol.Models.ViewModels;





public interface ISymbolDetailViewModel
{

    State State { get; }
    Symbol Symbol { get; }
    List<Symbol> RelatedSymbols { get; }
    

    string? ContentTitle { get; }
    string? ContentIntroText { get; }
    string? Author { get; }
    DateTime? DateModified { get; }
    int? AdoptedYear { get; }


    List<IContentSection>? Sections { get; }
    List<ISource>? Sources { get; }
    List<IFaqItem>? Faq { get; }


    bool HasSections { get; }
    bool HasSources { get; }
    bool HasFaq { get; }

    IReadOnlyList<QuickFactItem>? QuickFacts { get; }


    SymbolColorScheme Colors { get; }
    

    string SymbolTypeName { get; }
    string SymbolTypeSlug { get; }
    string SymbolTypePlural { get; }
    string SymbolTypeIcon { get; }
}




public class SymbolColorScheme
{
    public string Primary { get; set; } = "blue-600";
    public string PrimaryHover { get; set; } = "blue-700";
    public string GradientFrom { get; set; } = "from-blue-50";
    public string GradientTo { get; set; } = "to-indigo-50";
    public string Border { get; set; } = "border-blue-200";
    public string BorderHover { get; set; } = "border-blue-500";
    public string Text { get; set; } = "text-blue-700";
    public string TextDark { get; set; } = "text-blue-900";
    public string Bg { get; set; } = "bg-blue-50";
    public string BgAccent { get; set; } = "bg-blue-600";
    public string Icon { get; set; } = "text-blue-600";
    public string BadgeBg { get; set; } = "bg-blue-100";
    public string BadgeText { get; set; } = "text-blue-800";
    public string ProgressBar { get; set; } = "from-slate-800 via-blue-900 to-slate-900";
    

    public static SymbolColorScheme Blue => new()
    {
        Primary = "blue-600", PrimaryHover = "blue-700",
        GradientFrom = "from-blue-50", GradientTo = "to-indigo-50",
        Border = "border-blue-200", BorderHover = "border-blue-500",
        Text = "text-blue-700", TextDark = "text-blue-900",
        Bg = "bg-blue-50", BgAccent = "bg-blue-600",
        Icon = "text-blue-600", BadgeBg = "bg-blue-100", BadgeText = "text-blue-800",
        ProgressBar = "from-slate-800 via-blue-900 to-slate-900"
    };
    
    public static SymbolColorScheme Green => new()
    {
        Primary = "green-600", PrimaryHover = "green-700",
        GradientFrom = "from-green-50", GradientTo = "to-emerald-50",
        Border = "border-green-200", BorderHover = "border-green-500",
        Text = "text-green-700", TextDark = "text-green-900",
        Bg = "bg-green-50", BgAccent = "bg-green-600",
        Icon = "text-green-600", BadgeBg = "bg-green-100", BadgeText = "text-green-800",
        ProgressBar = "from-green-800 via-emerald-900 to-green-900"
    };
    
    public static SymbolColorScheme Pink => new()
    {
        Primary = "pink-600", PrimaryHover = "pink-700",
        GradientFrom = "from-pink-50", GradientTo = "to-rose-50",
        Border = "border-pink-200", BorderHover = "border-pink-500",
        Text = "text-pink-700", TextDark = "text-pink-900",
        Bg = "bg-pink-50", BgAccent = "bg-pink-600",
        Icon = "text-pink-600", BadgeBg = "bg-pink-100", BadgeText = "text-pink-800",
        ProgressBar = "from-pink-800 via-rose-900 to-pink-900"
    };
    
    public static SymbolColorScheme Red => new()
    {
        Primary = "red-600", PrimaryHover = "red-700",
        GradientFrom = "from-red-50", GradientTo = "to-rose-50",
        Border = "border-red-200", BorderHover = "border-red-500",
        Text = "text-red-700", TextDark = "text-red-900",
        Bg = "bg-red-50", BgAccent = "bg-red-600",
        Icon = "text-red-600", BadgeBg = "bg-red-100", BadgeText = "text-red-800",
        ProgressBar = "from-red-800 via-rose-900 to-red-900"
    };
    
    public static SymbolColorScheme Amber => new()
    {
        Primary = "amber-600", PrimaryHover = "amber-700",
        GradientFrom = "from-amber-50", GradientTo = "to-yellow-50",
        Border = "border-amber-200", BorderHover = "border-amber-500",
        Text = "text-amber-700", TextDark = "text-amber-900",
        Bg = "bg-amber-50", BgAccent = "bg-amber-600",
        Icon = "text-amber-600", BadgeBg = "bg-amber-100", BadgeText = "text-amber-800",
        ProgressBar = "from-amber-800 via-yellow-900 to-amber-900"
    };
    
    public static SymbolColorScheme Orange => new()
    {
        Primary = "orange-600", PrimaryHover = "orange-700",
        GradientFrom = "from-orange-50", GradientTo = "to-amber-50",
        Border = "border-orange-200", BorderHover = "border-orange-500",
        Text = "text-orange-700", TextDark = "text-orange-900",
        Bg = "bg-orange-50", BgAccent = "bg-orange-600",
        Icon = "text-orange-600", BadgeBg = "bg-orange-100", BadgeText = "text-orange-800",
        ProgressBar = "from-orange-800 via-amber-900 to-orange-900"
    };
    
    public static SymbolColorScheme Cyan => new()
    {
        Primary = "cyan-600", PrimaryHover = "cyan-700",
        GradientFrom = "from-blue-50", GradientTo = "to-cyan-50",
        Border = "border-cyan-200", BorderHover = "border-cyan-500",
        Text = "text-cyan-700", TextDark = "text-cyan-900",
        Bg = "bg-cyan-50", BgAccent = "bg-cyan-600",
        Icon = "text-cyan-600", BadgeBg = "bg-cyan-100", BadgeText = "text-cyan-800",
        ProgressBar = "from-blue-800 via-cyan-900 to-blue-900"
    };
    
    public static SymbolColorScheme Purple => new()
    {
        Primary = "purple-600", PrimaryHover = "purple-700",
        GradientFrom = "from-purple-50", GradientTo = "to-violet-50",
        Border = "border-purple-200", BorderHover = "border-purple-500",
        Text = "text-purple-700", TextDark = "text-purple-900",
        Bg = "bg-purple-50", BgAccent = "bg-purple-600",
        Icon = "text-purple-600", BadgeBg = "bg-purple-100", BadgeText = "text-purple-800",
        ProgressBar = "from-purple-800 via-violet-900 to-purple-900"
    };
    
    public static SymbolColorScheme Indigo => new()
    {
        Primary = "indigo-600", PrimaryHover = "indigo-700",
        GradientFrom = "from-indigo-50", GradientTo = "to-blue-50",
        Border = "border-indigo-200", BorderHover = "border-indigo-500",
        Text = "text-indigo-700", TextDark = "text-indigo-900",
        Bg = "bg-indigo-50", BgAccent = "bg-indigo-600",
        Icon = "text-indigo-600", BadgeBg = "bg-indigo-100", BadgeText = "text-indigo-800",
        ProgressBar = "from-indigo-800 via-blue-900 to-indigo-900"
    };

    public static SymbolColorScheme Slate => new()
    {
        Primary = "slate-600",
        PrimaryHover = "slate-700",
        GradientFrom = "from-slate-50",
        GradientTo = "to-zinc-50",
        Border = "border-slate-200",
        BorderHover = "border-slate-500",
        Text = "text-slate-700",
        TextDark = "text-slate-900",
        Bg = "bg-slate-50",
        BgAccent = "bg-slate-600",
        Icon = "text-slate-600",
        BadgeBg = "bg-slate-100",
        BadgeText = "text-slate-800",
        ProgressBar = "from-slate-800 via-slate-900 to-slate-950"
    };


}
