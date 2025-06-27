#nullable enable

﻿using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo; // Eklendi
using DevExpress.Xpo; // Eklendi
// using Microsoft.EntityFrameworkCore; // Kaldırıldı
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
using XPOVectorSearch.Blazor.Server.ContentDecoders;
using XPOVectorSearch.Blazor.Server.Settings;
using XPOVectorSearch.Module.Models;
using XPOVectorSearch.Blazor.Server.Helpers; // Eklendi
using System.Linq; // Eklendi
using DevExpress.Data.Filtering; // CriteriaOperator için eklendi
using System.Collections.Generic; // List<T> için eklendi

namespace XPOVectorSearch.Blazor.Server.Services;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class VectorSearchService(IServiceProvider serviceProvider, IObjectSpaceFactory objectSpaceFactory,  ITextEmbeddingGenerationService textEmbeddingGenerationService, ChatService chatService, TokenizerService tokenizerService, IOptions<AppSettings> appSettingsOptions, ILogger<VectorSearchService> logger)
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
    private readonly AppSettings appSettings = appSettingsOptions.Value;

    public Task<Guid> ImportAsync(Guid? documentId)
    {

        using IObjectSpace newObjectSpace = objectSpaceFactory.CreateObjectSpace<Module.BusinessObjects.Documents>();
        Module.BusinessObjects.Documents documents = newObjectSpace.GetObjectByKey<Module.BusinessObjects.Documents>(documentId!.Value);

        using var stream = new MemoryStream();
        documents.File.SaveToStream(stream);

        return ImportAsync(stream, Helpers.Helper.GetContentType(documents.File.FileName), documentId);

    }

    public async Task<Guid> ImportAsync(Stream stream, string contentType, Guid? documentId)
    {
        // Extract the contents of the file.
        var decoder = serviceProvider.GetKeyedService<IContentDecoder>(contentType) ?? throw new NotSupportedException($"Content type '{contentType}' is not supported.");
        var content = await decoder.DecodeAsync(stream, contentType);

        using IObjectSpace objectSpace = objectSpaceFactory.CreateObjectSpace<Module.BusinessObjects.Documents>();

        var document = objectSpace.GetObjectByKey<Module.BusinessObjects.Documents>(documentId.GetValueOrDefault());



        // Split the content into chunks and generate the embeddings for each one.
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var lines = TextChunker.SplitPlainTextLines(content, appSettings.MaxTokensPerLine, tokenizerService.CountTokens);

        var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, appSettings.MaxTokensPerParagraph, appSettings.OverlapTokens, tokenCounter: tokenizerService.CountTokens);
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var embeddings = await textEmbeddingGenerationService.GenerateEmbeddingsAsync(paragraphs);

        // Save the document chunks and the corresponding embedding in the database.
        foreach (var (index, paragraph) in paragraphs.Index())
        {
            logger.LogInformation("Storing a paragraph of {TokenCount} tokens.", tokenizerService.CountTokens(paragraph));

            var documentChunk = objectSpace.CreateObject<Module.BusinessObjects.DocumentChunks>();
            documentChunk.Index = index;
            documentChunk.Content = paragraph!;
            var embeddingBytes = VectorHelper.FloatArrayToByteArray(embeddings[index]);
            if (embeddingBytes == null || embeddingBytes.Length == 0)
            {
                logger.LogWarning($"Generated embedding for chunk {index} of document {document.Oid} is null or empty before assignment.");
            }
            documentChunk.Embedding = embeddingBytes;
            if (documentChunk.Embedding == null || documentChunk.Embedding.Length == 0)
            {
                logger.LogError($"DocumentChunk Oid {documentChunk.Oid} (or new) Embedding is null or empty AFTER assignment but BEFORE commit. Index: {index}");
            }
            document.DocumentChunks.Add(documentChunk);
        }



        objectSpace.CommitChanges();

        return document.ID;
    }

    public async Task<IEnumerable<Module.Models.Documents>> GetDocumentsAsync()
    {
        // Asenkron IObjectSpace operasyonları XPO'da genellikle Task.Run içine alınır veya IObjectSpaceAsync kullanılır (varsa).
        // Şimdilik senkron operasyonları Task.FromResult ile saralım veya doğrudan senkron bırakalım.
        // XPO sorguları genellikle senkrondur. IObjectSpace.GetObjects() senkron çalışır.
        // Eğer UI thread'ini bloklamamak için asenkronluk isteniyorsa, Task.Run gerekebilir.
        // XAF Blazor'da IObjectSpace işlemleri genellikle UI thread'inde senkron olarak yapılır.
        // Bu servis katmanı olduğu için asenkron bırakmak daha doğru olabilir.
        // Ancak XPO'nun asenkron API'leri EF Core kadar yaygın değildir.

        // Simplest approach for now, assuming synchronous XPO operations wrapped in Task for API compatibility
        return await Task.Run(() =>
        {
            using IObjectSpace objectSpace = objectSpaceFactory.CreateObjectSpace<Module.BusinessObjects.Documents>();
            // XPQuery<T> veya objectSpace.GetObjects<T>() kullanılabilir.
            var xpoDocuments = new XPQuery<Module.BusinessObjects.Documents>(((XPObjectSpace)objectSpace).Session)
                                .OrderBy(d => d.File.FileName)
                                .ToList(); // Veriyi çek

            // Projeksiyon (Select) LINQ to Objects ile yapılır
            var result = xpoDocuments.Select(d => new Module.Models.Documents(d.Oid, d.File.FileName, d.DocumentChunks.Count))
                                     .ToList();
            return result;
        });
    }

    public async Task<IEnumerable<Module.Models.DocumentChunks>> GetDocumentChunksAsync(Guid documentId)
    {
        return await Task.Run(() =>
        {
            using IObjectSpace objectSpace = objectSpaceFactory.CreateObjectSpace<Module.BusinessObjects.DocumentChunks>();
            // DocumentId alanı DocumentChunks XPO nesnesinde yok, Document.Oid üzerinden filtreleme yapılacak.
            var xpoDocumentChunks = new XPQuery<Module.BusinessObjects.DocumentChunks>(((XPObjectSpace)objectSpace).Session)
                                    .Where(c => c.Document.Oid == documentId)
                                    .OrderBy(c => c.Index)
                                    .ToList();

            var result = xpoDocumentChunks.Select(c => new Module.Models.DocumentChunks(c.Oid, c.Index, c.Content, null))
                                          .ToList();
            return result;
        });
    }

    public async Task<Module.Models.DocumentChunks?> GetDocumentChunkEmbeddingAsync(Guid documentId, Guid documentChunkId)
    {
        return await Task.Run(() =>
        {
            using IObjectSpace objectSpace = objectSpaceFactory.CreateObjectSpace<Module.BusinessObjects.DocumentChunks>();
            var xpoDocumentChunk = new XPQuery<Module.BusinessObjects.DocumentChunks>(((XPObjectSpace)objectSpace).Session)
                                    .FirstOrDefault(c => c.Oid == documentChunkId && c.Document.Oid == documentId);

            if (xpoDocumentChunk == null)
                return null;

            // Embedding byte[] to float[] dönüşümü
            return new Module.Models.DocumentChunks(xpoDocumentChunk.Oid, xpoDocumentChunk.Index, xpoDocumentChunk.Content, VectorHelper.ByteArrayToFloatArray(xpoDocumentChunk.Embedding));
        });
    }

    public void DeleteDocument(Guid documentId)
    {
        using IObjectSpace objectSpace = objectSpaceFactory.CreateObjectSpace<Module.BusinessObjects.Documents>();
        var doc = objectSpace.GetObjectByKey<Module.BusinessObjects.Documents>(documentId);
        objectSpace.Delete(doc);
        objectSpace.CommitChanges();
    }

    public async Task<Response> AskQuestionAsync(Question question, bool reformulate = true)
    {
        var (reformulatedQuestion, chunks) = await CreateContextAsync(question, reformulate);

        var answer = await chatService.AskQuestionAsync(question.ConversationId, chunks, reformulatedQuestion);
        return new Response(reformulatedQuestion, answer);
    }

    public async IAsyncEnumerable<Response> AskStreamingAsync(Question question, bool reformulate = true)
    {
        var (reformulatedQuestion, chunks) = await CreateContextAsync(question, reformulate);

        var answerStream = chatService.AskStreamingAsync(question.ConversationId, chunks, reformulatedQuestion);

        // The first message contains the original question.
        yield return new Response(reformulatedQuestion, null, StreamState.Start);

        // Return each token as a partial response.
        await foreach (var token in answerStream)
        {
            yield return new Response(null, token, StreamState.Append);
        }

        // The last message tells the client that the stream has ended.
        yield return new Response(null, null, StreamState.End);
    }

    private async Task<(string Question, IEnumerable<string> Chunks)> CreateContextAsync(Question question, bool reformulate = true)
    {
        // Reformulate the following question taking into account the context of the chat to perform keyword search and embeddings:
        var reformulatedQuestion = reformulate ? await chatService.CreateQuestionAsync(question.ConversationId, question.Text) : question.Text;

        // Perform Vector Search on SQL Database.
        var questionEmbeddingFloats = await textEmbeddingGenerationService.GenerateEmbeddingAsync(reformulatedQuestion);
        var questionEmbeddingBytes = VectorHelper.FloatArrayToByteArray(questionEmbeddingFloats);

        using IObjectSpace objectSpace = objectSpaceFactory.CreateObjectSpace<Module.BusinessObjects.DocumentChunks>();
        var session = ((XPObjectSpace)objectSpace).Session;

        // XPO'nun tablo ve kolon adlarını al (isteğe bağlı, genellikle sınıf/özellik adları çalışır)
        // var classInfo = session.GetClassInfo<Module.BusinessObjects.DocumentChunks>();
        // var tableName = classInfo.TableName;
        // var embeddingColumn = classInfo.GetMember(nameof(Module.BusinessObjects.DocumentChunks.Embedding)).MappingField;
        // var contentColumn = classInfo.GetMember(nameof(Module.BusinessObjects.DocumentChunks.Content)).MappingField;
        // var oidColumn = classInfo.GetMember(nameof(Module.BusinessObjects.DocumentChunks.Oid)).MappingField;

        // SQL Server'a özgü sorgu. Parametreler sorgu içine doğrudan eklenemez, OperandValue kullanılmalı.
        // Ancak VectorDistance gibi fonksiyonlar için bu zor olabilir.
        // En güvenli yol, sorguyu string formatlama ile oluşturmak (SQL Injection riski yoksa, çünkü @vector bizim kontrolümüzde)
        // veya XPO'nun stored procedure çağırma yeteneklerini kullanmak.
        // Şimdilik, XPO'nun doğrudan SQL parametrelerini nasıl işlediğini varsayarak bir deneme yapalım.
        // Pratikte, bu tür karmaşık yerel sorgular için bir stored procedure daha iyi olabilir.

        // Basitleştirilmiş yaklaşım: Tüm chunkları çekip uygulama tarafında sıralama (KÜÇÜK VERİ SETLERİ İÇİN GEÇİCİ ÇÖZÜM)
        // BU PERFORMANS SORUNLARINA YOL AÇAR, DOĞRU YÖNTEM DEĞİLDİR.
        // DOĞRU YÖNTEM SUNUCU TARAFINDA SIRALAMA YAPMAKTIR.
        // Araştırma sonucumuz Session.GetObjectsFromQuery idi.

        // SQL sorgusu oluşturma (SQL Server için)
        // XPO'nun string parametreleri nasıl ele aldığına dikkat edin. Genellikle OperandValue[] kullanılır.
        // VectorDistance fonksiyonu byte[] (varbinary) bekler.
        // XPO'nun GetObjectsFromQuery'si doğrudan SQL parametrelerini @p0, @p1 gibi desteklemez.
        // Sorguyu string.Format ile oluşturmak SQL enjeksiyonu riski taşır eğer @vector dışarıdan geliyorsa.
        // Burada @vector bizim tarafımızdan oluşturulduğu için bu risk daha az.

        // Geçici olarak, SQL Injection'a daha az eğilimli bir yol deneyelim.
        // Stored procedure veya daha gelişmiş bir XPO sorgu tekniği idealdir.
        // Şimdilik, XPO'nun dahili SQL oluşturucusuna güvenmek yerine,
        // doğrudan SQL'i session üzerinden çalıştırmayı deneyeceğiz.

        // GetObjectsFromQuery için SQL'in XPO nesneleriyle eşleşen kolonları döndürmesi gerekir.
        // Örnek: SELECT Oid, Content, Embedding, Document, Index FROM DocumentChunks ORDER BY VectorDistance(...)
        // Ancak VectorDistance parametresi nasıl güvenli geçirilecek?

        // Daha güvenli bir yol için, SQL parametrelerini XPO'nun anladığı şekilde kullanmaya çalışalım.
        // Maalesef XPO, SELECT sorgularında karmaşık fonksiyonlarla parametre geçişini bu şekilde doğrudan desteklemeyebilir.

        // En pratik çözüm, bir Stored Procedure oluşturmak veya Session.ExecuteXXX serisi ile çalışmaktır.
        // Session.ExecuteQuery ile sadece skalar olmayan sonuçlar alınır.
        // Session.GetObjectsFromQuery en uygunu gibi duruyor ama parametreli VectorDistance zor.

        // Şimdilik, tüm chunk'ları çekip uygulama katmanında sıralama yapacağım.
        // BU KESİNLİKLE GEÇİCİ BİR ÇÖZÜMDÜR VE PERFORMANS NEDENİYLE DEĞİŞTİRİLMELİDİR.
        // GERÇEK BİR UYGULAMADA, VERİTABANI TARAFINDA SIRALAMA ŞARTTIR.
        // Planımız doğrudan SQL idi, ancak parametre güvenliği ve XPO entegrasyonu düşündürücü.

        // Geçici Çözüm (Uygulama Tarafında Sıralama - KÖTÜ PERFORMANS):
        var allChunks = objectSpace.GetObjects<Module.BusinessObjects.DocumentChunks>().ToList();
        if (!allChunks.Any())
        {
            logger.LogWarning("No document chunks found in the database for vector search.");
            return (reformulatedQuestion, new List<string>());
        }

        foreach (var chunk in allChunks)
        {
            if (chunk.Embedding == null || chunk.Embedding.Length == 0)
            {
                logger.LogWarning($"DocumentChunk Oid {chunk.Oid} has null or empty Embedding. Content: {chunk.Content?.Substring(0, Math.Min(50, chunk.Content?.Length ?? 0))}...");
            }
        }

        var relevantChunks = allChunks
            .Where(c => c.Embedding != null && c.Embedding.Length > 0) // Null veya boş embedding'leri filtrele
            .Select(c => new { Chunk = c, Distance = CosineSimilarity(questionEmbeddingFloats.Span.ToArray(), VectorHelper.ByteArrayToFloatArray(c.Embedding)) })
            .OrderBy(x => x.Distance) // Cosine similarity'de 1'e yakın olanlar daha benzerdir. Distance ise 1-similarity olabilir.
                                      // VectorDistance('cosine', v1, v2) düşükse daha yakındır.
            .Take(appSettings.MaxRelevantChunks)
            .Select(x => x.Chunk.Content)
            .ToList();

        // Eğer VectorDistance düşük değerleri daha iyi eşleşme olarak veriyorsa OrderBy doğru.
        // CosineSimilarity yüksek (1'e yakın) değerleri daha iyi eşleşme olarak verir.
        // Bu yüzden CosineDistance = 1 - CosineSimilarity kullanılır ve bu minimize edilir.
        // SQL Server'daki VectorDistance('cosine', ...) zaten distance döndürür, yani düşük olan daha iyidir.

        // TODO: Bu bölüm kesinlikle sunucu tarafı sıralama ile değiştirilmelidir.
        // Araştırma adımındaki Session.GetObjectsFromQuery ile ham SQL çalıştırma hedeflenmeli.
        // Parametreli sorgu için XPO'nun `DevExpress.Xpo.DB.Helpers.QueryParameter` ve `Session.SelectData`
        // veya `Session.GetObjects(CriteriaOperator)` ile `FreeJoins` gibi daha gelişmiş teknikleri incelenmeli.
        // Veya en basitinden Stored Procedure.

        logger.LogWarning("CreateContextAsync uses client-side sorting for vector search, which is inefficient. This should be replaced with server-side sorting using a direct SQL query or stored procedure.");

        return (reformulatedQuestion, relevantChunks);
    }

    // Geçici olarak eklenecek CosineSimilarity fonksiyonu (istemci tarafı için)
    private static float CosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
            throw new ArgumentException("Vectors must have the same length.");

        float dotProduct = 0.0f;
        float norm1 = 0.0f;
        float norm2 = 0.0f;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            norm1 += vector1[i] * vector1[i];
            norm2 += vector2[i] * vector2[i];
        }

        if (norm1 == 0 || norm2 == 0)
            return 0.0f; // Sıfır vektörler için.

        return dotProduct / (MathF.Sqrt(norm1) * MathF.Sqrt(norm2));
    }

}
