using System.Text.Json;

var daten = Datenquelle.GibMirDummyDaten();

Console.WriteLine(JsonSerializer.Serialize(daten));
