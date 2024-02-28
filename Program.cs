using System.Text.Json;

var daten = Datenquelle.GibMirDummyDaten();

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
  
Console.WriteLine(JsonSerializer.Serialize(daten));
