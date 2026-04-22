using LiteDB;

namespace CtrlCV
{
    internal sealed class PersistedClipboardItem
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();

        public int OrderIndex { get; set; }

        public string ItemType { get; set; } = "Text";

        public string? Text { get; set; }

        public byte[]? InlineImageBytes { get; set; }

        public string? ImageFileId { get; set; }

        public DateTime CopiedAtUtc { get; set; }
    }
}
