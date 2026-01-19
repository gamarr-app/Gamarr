using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Indexers.Newznab
{
    public static class NewznabCategoryFieldOptionsConverter
    {
        public static List<FieldSelectOption<int>> GetFieldSelectOptions(List<NewznabCategory> categories)
        {
            // Categories not relevant for Gamarr (games) - ignore Movies, Audio, TV, XXX, Books
            var ignoreCategories = new[] { 2000, 3000, 5000, 6000, 7000 };

            // And maybe relevant for specific users
            var unimportantCategories = new[] { 0, 8000 };

            var result = new List<FieldSelectOption<int>>();

            if (categories == null)
            {
                // Fetching categories failed, use default Newznab game categories
                categories = new List<NewznabCategory>();
                categories.Add(new NewznabCategory
                {
                    Id = 1000,
                    Name = "Console",
                    Subcategories = new List<NewznabCategory>
                    {
                        new NewznabCategory { Id = 1010, Name = "NDS" },
                        new NewznabCategory { Id = 1020, Name = "PSP" },
                        new NewznabCategory { Id = 1030, Name = "Wii" },
                        new NewznabCategory { Id = 1040, Name = "Xbox" },
                        new NewznabCategory { Id = 1050, Name = "Xbox 360" },
                        new NewznabCategory { Id = 1060, Name = "Wiiware" },
                        new NewznabCategory { Id = 1070, Name = "Xbox 360 DLC" },
                        new NewznabCategory { Id = 1080, Name = "PS3" },
                        new NewznabCategory { Id = 1090, Name = "Other" },
                        new NewznabCategory { Id = 1110, Name = "3DS" },
                        new NewznabCategory { Id = 1120, Name = "PS Vita" },
                        new NewznabCategory { Id = 1130, Name = "WiiU" },
                        new NewznabCategory { Id = 1140, Name = "Xbox One" },
                        new NewznabCategory { Id = 1150, Name = "PS4" },
                        new NewznabCategory { Id = 1180, Name = "PS5" },
                        new NewznabCategory { Id = 1190, Name = "Switch" }
                    }
                });
                categories.Add(new NewznabCategory
                {
                    Id = 4000,
                    Name = "PC",
                    Subcategories = new List<NewznabCategory>
                    {
                        new NewznabCategory { Id = 4010, Name = "0day" },
                        new NewznabCategory { Id = 4020, Name = "ISO" },
                        new NewznabCategory { Id = 4030, Name = "Mac" },
                        new NewznabCategory { Id = 4040, Name = "Mobile-Other" },
                        new NewznabCategory { Id = 4050, Name = "Games" },
                        new NewznabCategory { Id = 4060, Name = "Mobile-iOS" },
                        new NewznabCategory { Id = 4070, Name = "Mobile-Android" }
                    }
                });
            }

            foreach (var category in categories.Where(cat => !ignoreCategories.Contains(cat.Id)).OrderBy(cat => unimportantCategories.Contains(cat.Id)).ThenBy(cat => cat.Id))
            {
                result.Add(new FieldSelectOption<int>
                {
                    Value = category.Id,
                    Name = category.Name,
                    Hint = $"({category.Id})"
                });

                if (category.Subcategories != null)
                {
                    foreach (var subcat in category.Subcategories.OrderBy(cat => cat.Id))
                    {
                        result.Add(new FieldSelectOption<int>
                        {
                            Value = subcat.Id,
                            Name = subcat.Name,
                            Hint = $"({subcat.Id})",
                            ParentValue = category.Id
                        });
                    }
                }
            }

            return result;
        }
    }
}
