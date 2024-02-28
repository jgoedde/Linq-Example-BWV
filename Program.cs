using System.Text.Json;

/*
daten.catalogItems
daten.catalogBrands
daten.catalogTypes
daten.basketItems,
daten.orders,
daten.buyers,
daten.paymentMethods,
daten.baskets
*/
var daten = Datenquelle.GibMirDummyDaten();
  
JsonSerializerOptions options = new() { IncludeFields = true };
var serializedTuple = JsonSerializer.Serialize(daten, options);
Console.WriteLine(serializedTuple);

// https://jsonformatter.org/json-viewer - Für den Fall, dass man sich die Daten visualisieren möchte
// using System.Linq
