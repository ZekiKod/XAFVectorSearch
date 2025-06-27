#nullable enable
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl; // EF kalktı
using DevExpress.Xpo; // Eklendi

namespace XPOVectorSearch.Module.BusinessObjects // Ad alanı güncellendi
{
    [DefaultClassOptions]
    public partial class DocumentChunks : BaseObject // XPObject veya BaseObject
    {
        public DocumentChunks(Session session) : base(session) { } // XPO Constructor

        private int _index;
        public int Index
        {
            get => _index;
            set => SetPropertyValue(nameof(Index), ref _index, value);
        }

        private string _content;
        [Size(SizeAttribute.Unlimited)] // İçeriğin uzun olabileceğini belirtir
        public string Content
        {
            get => _content;
            set => SetPropertyValue(nameof(Content), ref _content, value);
        }

        private byte[] _embedding; // float[] yerine byte[]
        [Size(SizeAttribute.Unlimited)] // Embedding verisinin boyutu büyük olabilir
        [DbType("bytea")] // PostgreSQL için açıkça belirtildi
        public byte[] Embedding // Uygulama katmanında float[] <-> byte[] dönüşümü yapılacak
        {
            get => _embedding;
            set => SetPropertyValue(nameof(Embedding), ref _embedding, value);
        }

        private Documents _document;
        [Association("Documents-DocumentChunks")] // Assosiasyon adı (Documents sınıfındaki ile aynı olmalı)
        public Documents Document
        {
            get => _document;
            set => SetPropertyValue(nameof(Document), ref _document, value);
        }
        // Guid DocumentId alanı, Document ilişkisi üzerinden yönetileceği için ayrıca tutmaya gerek yok.
        // Guid ID özelliği BaseObject tarafından Oid olarak sağlanır.
    }
}

namespace XPOVectorSearch.Module.Models // Ad alanı güncellendi
{
    // DTO'da Embedding float[]? olarak kalabilir, servisler bu dönüşümü yapacaktır.
    public record class DocumentChunks(Guid Id, int Index, string Content, float[]? Embedding = null);
}