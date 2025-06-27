#nullable disable
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl; // EF kalktı
using DevExpress.Xpo; // Eklendi
using System.Collections.ObjectModel; // XPCollection için System.ComponentModel da gerekebilir.

namespace XPOVectorSearch.Module.BusinessObjects // Ad alanı güncellendi
{
    [DefaultClassOptions]
    public partial class Documents : BaseObject // XPObject veya BaseObject kullanılabilir. BaseObject daha yaygın XAF'ta.
    {
        public Documents(Session session) : base(session) { } // XPO Constructor

        private FileData _file;
        [ExpandObjectMembers(ExpandObjectMembers.Never)]
        [FileTypeFilter("File docx,txt", 1, "*.txt", "*.docx")]
        [FileTypeFilter("File Pdf", 2, "*.pdf")]
        [Aggregated] // FileData genellikle ana nesne ile birlikte kaydedilir.
        public FileData File
        {
            get => _file;
            set => SetPropertyValue(nameof(File), ref _file, value);
        }

        [Association("Documents-DocumentChunks")] // Assosiasyon adı
        [Aggregated] // DocumentChunks'lar Documents ile birlikte yönetilecekse.
        public XPCollection<DocumentChunks> DocumentChunks
        {
            get { return GetCollection<DocumentChunks>(nameof(DocumentChunks)); }
        }

        // Guid ID özelliği BaseObject veya XPObject tarafından Oid olarak sağlanır.
        // Eğer özel bir Guid ID isteniyorsa, [Key(AutoGenerate = true)] public Guid ID { get; set; } kullanılabilir
        // ancak bu durumda XPObject yerine XPCustomObject'tan türemek ve Oid'i manuel yönetmek gerekebilir.
        // Şimdilik BaseObject'in Oid'ini kullanıyoruz.
    }
}

namespace XPOVectorSearch.Module.Models // Ad alanı güncellendi
{
    public record class Documents(Guid Id, string Name, int ChunkCount);
}