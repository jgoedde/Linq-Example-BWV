using System.Text.Json;

var daten = Datenquelle.GibMirDummyDaten();A1A5-9793

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
