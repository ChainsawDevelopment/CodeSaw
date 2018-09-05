namespace CodeSaw.RepositoryApi
{
    public class AwardEmoji
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public UserInfo User { get; set; }

        public bool Is(EmojiType emojiTypeType) => Name == emojiTypeType.ToString().ToLower();
    }
}