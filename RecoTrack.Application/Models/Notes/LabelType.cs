namespace RecoTrack.Application.Models.Notes
{
     public enum LabelType
     {
         Important,
         Favourite,
         Pinned
     }

     public static class LabelTypeExtensions
     {
         public static bool TryNormalize(string? input, out string normalized)
         {
             normalized = string.Empty;

             if (string.IsNullOrWhiteSpace(input)) return false;

             var s = input.Trim();

             // Accept 'favorite' spelling and normalize to 'Favourite'
             if (string.Equals(s, "favorite", System.StringComparison.OrdinalIgnoreCase))
             {
                 normalized = LabelType.Favourite.ToString();
                 return true;
             }

             // Try parse enum case-insensitive
             if (System.Enum.TryParse<LabelType>(s, true, out var label))
             {
                 normalized = label.ToString();
                 return true;
             }

              return false;
         }
     }
}
