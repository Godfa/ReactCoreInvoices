# Database Management

## Tietokannan Seedaus

### Automaattinen Seedaus Käynnistyksen Yhteydessä

API suorittaa automaattisesti tietokannan seedauksen joka kerta kun palvelin käynnistyy (`Program.cs`):

1. **Migrations**: Kaikki odottavat migraatiot suoritetaan
2. **Seed Data**: Käyttäjät, roolit, creditorit ja invoicet seedataan

#### Development vs. Production

**Development-ympäristössä:**
- Kaikki invoicet poistetaan ja luodaan uudelleen joka kerta kun serveri käynnistyy
- Tämä mahdollistaa nopean testauksen ja kehityksen

**Production-ympäristössä:**
- Invoiceja **EI** poisteta automaattisesti
- Seed-data luodaan vain jos sitä ei vielä ole olemassa

Logiikka löytyy tiedostosta `Persistence/Seed.cs`:

```csharp
// Clear existing invoices only in Development environment
if (environment.IsDevelopment() && context.Invoices.Any())
{
    var existingInvoices = await context.Invoices
        .Include(i => i.ExpenseItems)
            .ThenInclude(ei => ei.Payers)
        .Include(i => i.Participants)
        .ToListAsync();

    context.Invoices.RemoveRange(existingInvoices);
    await context.SaveChangesAsync();
}

// Only seed invoices if none exist
if (context.Invoices.Any()) return;
```

## Tietokannan Uudelleenseedaus Tuotannossa

### Vaihtoehto 1: Admin API Endpoint (Suositus)

Admin-käyttäjät voivat uudelleenseedata tietokannan API-endpointin kautta.

**Endpoint:** `POST /api/admin/reseed-database`

**Autorisointi:** Vaatii Admin-roolin

**Käyttö cURL:illa:**
```bash
curl -X POST https://your-api.com/api/admin/reseed-database \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

**Käyttö Postmanilla:**
1. Luo uusi POST request
2. URL: `https://your-api.com/api/admin/reseed-database`
3. Headers:
   - `Authorization: Bearer YOUR_ADMIN_JWT_TOKEN`
4. Lähetä request

**Vastaus onnistuessa:**
```json
{
  "message": "Database reseeded successfully"
}
```

**Vastaus epäonnistuessa:**
```json
{
  "message": "Failed to reseed database: [error message]"
}
```

**Mitä tapahtuu:**
1. Kaikki invoicet poistetaan (mukaan lukien ExpenseItems, Payers, Participants)
2. Seed-data luodaan uudelleen
3. Käyttäjät ja creditorit säilyvät ennallaan

**Koodi:** `API/Controllers/AdminController.cs`

### Vaihtoehto 2: Entity Framework Migrations

Voit luoda custom migrationin joka tyhjentää invoicet:

```bash
# Luo uusi migraatio
dotnet ef migrations add ClearInvoiceData --project Persistence --startup-project API

# Muokkaa migration-tiedostoa lisäämään SQL-komennot
# Up-metodissa:
migrationBuilder.Sql(@"
    DELETE FROM ExpenseItemPayers;
    DELETE FROM InvoiceParticipants;
    DELETE FROM ExpenseItems;
    DELETE FROM Invoices;
");

# Suorita migraatio
dotnet ef database update --project API
```

### Vaihtoehto 3: SQL Query Suoraan Tietokannassa

Jos sinulla on pääsy tietokantaan (Azure Data Studio, SQL Server Management Studio):

```sql
-- Poista kaikki invoice-data
DELETE FROM ExpenseItemPayers;
DELETE FROM InvoiceParticipants;
DELETE FROM ExpenseItems;
DELETE FROM Invoices;

-- Seuraavan kerran kun API käynnistyy, se luo seed-datan uudelleen
```

**Huom:** Tämän jälkeen seed-data luodaan automaattisesti kun API seuraavan kerran käynnistyy.

### Vaihtoehto 4: Restart API Serveriä (Vain Development)

Development-ympäristössä riittää että:
1. Pysäytät API-serverin (Ctrl+C)
2. Käynnistät sen uudelleen (`dotnet run`)

Tämä poistaa automaattisesti kaikki invoicet ja luo ne uudelleen.

## Default Seed Data

### Käyttäjät
- **epi** - Admin-käyttäjä (salasana: `Pa$$w0rd`)
- leivo, jaapu, timo, jhattu, urpi, zeip, antti, sakke, lasse (salasana: `Pa$$w0rd`)

### Creditors
- Epi, Leivo, Jaapu, Timo, JHattu, Urpi, Zeip, Antti, Sakke, Lasse

### Invoices
- **Mökkilan 68** - LAN-tapahtuma Nousiaisissa
  - Participants: Epi, JHattu, Leivo, Timo, Jaapu, Urpi, Zeip (Usual Suspects)
  - Expense Item: "Kauppalista" (415.24€)

## Turvallisuus

⚠️ **Tärkeää:**
- `/api/admin/reseed-database` endpoint vaatii Admin-roolin
- Production-ympäristössä vain admin-käyttäjät voivat suorittaa uudelleenseedauksen
- Kaikki invoice-data poistetaan peruuttamattomasti!
- Varmista että sinulla on backup ennen uudelleenseedausta

## Troubleshooting

### Seed-data ei luotu
1. Tarkista että tietokannassa ei ole jo invoiceja
2. Tarkista API-lokit (`info: API.Program[0]`)
3. Varmista että creditorit on luotu ensin

### "Failed to reseed database" -virhe
1. Tarkista että käyttäjällä on Admin-rooli
2. Tarkista API-lokit tarkemman virheviestin saamiseksi
3. Varmista että tietokantayhteys toimii

### Development-ympäristössä invoicet eivät poistu
1. Tarkista `ASPNETCORE_ENVIRONMENT` ympäristömuuttuja
2. Pitäisi olla `Development`
3. Tarkista `launchSettings.json` tiedosto
