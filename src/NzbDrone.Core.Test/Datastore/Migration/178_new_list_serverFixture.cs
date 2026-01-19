using System.Text.Json;
using System.Text.Json.Serialization;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.ImportLists.GamarrList;
using NzbDrone.Core.ImportLists.GamarrList2.IMDbList;
using NzbDrone.Core.ImportLists.GamarrList2.StevenLu;
using NzbDrone.Core.ImportLists.StevenLu;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class new_list_serverFixture : MigrationTest<new_list_server>
    {
        private  JsonSerializerOptions _serializerSettings;

        [SetUp]
        public void Setup()
        {
            _serializerSettings = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                PropertyNameCaseInsensitive = true,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            _serializerSettings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true));
        }

        [TestCase("https://api.gamarr.video/v2")]
        [TestCase("https://api.gamarr.video/v2/")]
        [TestCase("https://staging.api.gamarr.video")]
        public void should_switch_some_gamarr_to_imdb(string url)
        {
            var db = WithMigrationTestDb(c =>
            {
                var rows = Builder<new_list_server.NetImportDefinition178>.CreateListOfSize(7)
                    .All()
                    .With(x => x.Implementation = typeof(GamarrListImport).Name)
                    .With(x => x.ConfigContract = typeof(GamarrListSettings).Name)
                    .TheFirst(1)
                    .With(x => x.Settings = JsonSerializer.Serialize(new new_list_server.GamarrListSettings177
                    {
                        APIURL = url,
                        Path = "/imdb/top250"
                    }, _serializerSettings))
                    .TheNext(1)
                    .With(x => x.Settings = JsonSerializer.Serialize(new new_list_server.GamarrListSettings177
                    {
                        APIURL = url,
                        Path = "/imdb/popular"
                    }, _serializerSettings))
                    .TheNext(1)
                    .With(x => x.Settings = JsonSerializer.Serialize(new new_list_server.GamarrListSettings177
                    {
                        APIURL = url,
                        Path = "/imdb/missing"
                    }, _serializerSettings))
                    .TheNext(1)
                    .With(x => x.Settings = JsonSerializer.Serialize(new new_list_server.GamarrListSettings177
                    {
                        APIURL = url,
                        Path = "/imdb/list?listId=ls001"
                    }, _serializerSettings))
                    .TheNext(1)
                    .With(x => x.Settings = JsonSerializer.Serialize(new new_list_server.GamarrListSettings177
                    {
                        APIURL = url,
                        Path = "/imdb/list?listId=ls00ad"
                    }, _serializerSettings))
                    .TheNext(1)
                    .With(x => x.Settings = JsonSerializer.Serialize(new new_list_server.GamarrListSettings177
                    {
                        APIURL = url,
                        Path = "/imdb/list?listId=ur002"
                    }, _serializerSettings))
                    .TheNext(1)
                    .With(x => x.Settings = JsonSerializer.Serialize(new new_list_server.GamarrListSettings177
                    {
                        APIURL = url,
                        Path = "/imdb/list?listId=ur002/"
                    }, _serializerSettings))
                    .BuildListOfNew();

                var i = 1;
                foreach (var row in rows)
                {
                    row.Id = i++;
                    c.Insert.IntoTable("NetImport").Row(row);
                }
            });

            var items = db.Query<new_list_server.NetImportDefinition178>("SELECT * FROM \"NetImport\"");

            items.Should().HaveCount(7);

            VerifyRow(items[0], typeof(IMDbListImport).Name, typeof(IMDbListSettings).Name, new IMDbListSettings { ListId = "top250" });
            VerifyRow(items[1], typeof(IMDbListImport).Name, typeof(IMDbListSettings).Name, new IMDbListSettings { ListId = "popular" });
            VerifyRow(items[2], typeof(GamarrListImport).Name, typeof(GamarrListSettings).Name, new GamarrListSettings { Url = url.TrimEnd('/') + "/imdb/missing" });
            VerifyRow(items[3], typeof(IMDbListImport).Name, typeof(IMDbListSettings).Name, new IMDbListSettings { ListId = "ls001" });
            VerifyRow(items[4], typeof(GamarrListImport).Name, typeof(GamarrListSettings).Name, new GamarrListSettings { Url = url.TrimEnd('/') + "/imdb/list?listId=ls00ad" });
            VerifyRow(items[5], typeof(IMDbListImport).Name, typeof(IMDbListSettings).Name, new IMDbListSettings { ListId = "ur002" });
            VerifyRow(items[6], typeof(IMDbListImport).Name, typeof(IMDbListSettings).Name, new IMDbListSettings { ListId = "ur002" });
        }

        public void should_switch_some_stevenlu_stevenlu2()
        {
            var rows = Builder<new_list_server.NetImportDefinition178>.CreateListOfSize(6)
                .All()
                .With(x => x.Implementation = typeof(StevenLuImport).Name)
                .With(x => x.ConfigContract = typeof(StevenLuSettings).Name)
                .TheFirst(1)
                .With(x => x.Settings = JsonSerializer.Serialize(new new_list_server.StevenLuSettings178
                    {
                        Link = "https://s3.amazonaws.com/popular-games/games.json"
                    }, _serializerSettings))
                .TheNext(1)
                .With(x => x.Settings = JsonSerializer.Serialize(new new_list_server.StevenLuSettings178
                    {
                        Link = "https://s3.amazonaws.com/popular-games/games-metacritic-min50.json"
                    }, _serializerSettings))
                .TheNext(1)
                .With(x => x.Settings = JsonSerializer.Serialize(new new_list_server.StevenLuSettings178
                    {
                        Link = "https://s3.amazonaws.com/popular-games/games-imdb-min8.json"
                    }, _serializerSettings))
                .TheNext(1)
                .With(x => x.Settings = JsonSerializer.Serialize(new new_list_server.StevenLuSettings178
                    {
                        Link = "https://s3.amazonaws.com/popular-games/games-rottentomatoes-min70.json"
                    }, _serializerSettings))
                .TheNext(1)
                .With(x => x.Settings = JsonSerializer.Serialize(new new_list_server.StevenLuSettings178
                    {
                        Link = "https://s3.amazonaws.com/popular-games/games-min70.json"
                    }, _serializerSettings))
                .TheNext(1)
                .With(x => x.Settings = JsonSerializer.Serialize(new new_list_server.StevenLuSettings178
                    {
                        Link = "https://aapjeisbaas.nl/api/v1/popular-games/imdb?fresh=True&max=20&rating=6&votes=50000"
                    }, _serializerSettings))
                .BuildListOfNew();

            var db = WithMigrationTestDb(c =>
            {
                var i = 1;
                foreach (var row in rows)
                {
                    row.Id = i++;
                    c.Insert.IntoTable("NetImport").Row(row);
                }
            });

            var items = db.Query<new_list_server.NetImportDefinition178>("SELECT * FROM \"NetImport\"");

            items.Should().HaveCount(5);

            VerifyRow(items[0], typeof(StevenLu2Import).Name, typeof(StevenLu2Import).Name, new StevenLu2Settings { Source = (int)StevenLuSource.Standard, MinScore = 5 });
            VerifyRow(items[1], typeof(StevenLu2Import).Name, typeof(StevenLu2Import).Name, new StevenLu2Settings { Source = (int)StevenLuSource.Metacritic, MinScore = 5 });
            VerifyRow(items[2], typeof(StevenLu2Import).Name, typeof(StevenLu2Import).Name, new StevenLu2Settings { Source = (int)StevenLuSource.Imdb, MinScore = 8 });
            VerifyRow(items[3], typeof(StevenLu2Import).Name, typeof(StevenLu2Import).Name, new StevenLu2Settings { Source = (int)StevenLuSource.RottenTomatoes, MinScore = 7 });

            // Bad formats so should not get changed
            VerifyRow(items[4], rows[4].Implementation, rows[4].ConfigContract, rows[4].Settings);
            VerifyRow(items[5], rows[5].Implementation, rows[5].ConfigContract, rows[5].Settings);
        }

        private void VerifyRow(new_list_server.NetImportDefinition178 row, string impl, string config, object settings)
        {
            row.Implementation.Should().Be(impl);
            row.ConfigContract.Should().Be(config);
            row.Settings.Should().Be(JsonSerializer.Serialize(settings, _serializerSettings));
        }
    }
}
