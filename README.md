# MyWallet

**MyWallet** to prosta aplikacja ASP .NET Core Web API do zarządzania portfelami, aktywami i transakcjami finansowymi. Została podzielona na kilka warstw, aby oddzielić logikę domenową od warstwy prezentacji i dostępu do danych.

---

## 🚀 Spis treści

1. [Architektura](#architektura)
2. [Struktura projektu](#struktura-projektu)
3. [Uruchomienie](#uruchomienie)
4. [Kontrolery i endpointy](#kontrolery-i-endpointy)
5. [Warstwa usług (Services)](#warstwa-usług-services)
6. [DTO i mapowanie](#dto-i-mapowanie)
7. [Dostęp do danych (EF Core)](#dostęp-do-danych-ef-core)
8. [Analiza indeksów do optymalizacji zapytań]
9. [Testy Postman](#testy-postman)
10. [Github Actions]
11. [Nasłuch endpointu]
12. [NBomber - masakracja api ku chwale ludzkości]
13. [Przykładowe działanie aplikacji - zrzuty ekranu]

---

## 🏗 Architektura

Aplikacja zbudowana jest zgodnie z zasadami **Clean Architecture** / **Onion Architecture**:

```
API (kontrolery)
  ↕
Services (IService + implementacje)
  ↕
Mappers (DTO ↔ Model)
  ↕
Data (DbContext + migracje)
  ↕
Models (encje domenowe)
```

Każda warstwa odpowiada za inną odpowiedzialność:

* **Controllers** – przyjmują i walidują żądania, zwracają odpowiedzi HTTP.
* **Services** – logika biznesowa: CRUD, obliczenia, współpraca z zewnętrznymi API.
* **Mappers** – transformacja między modelami domenowymi a DTO.
* **Data** – konfiguracja `ApplicationDbContext`, migracje, konfiguracja EF Core.
* **Models** – definicje encji (Asset, Portfolio, Transaction, User).

---

## 📂 Struktura projektu

```
MyWallet/
├─ Controllers/              # Web API endpoints
│  ├─ AssetController.cs
│  ├─ PortfolioController.cs
│  ├─ TransactionController.cs
│  ├─ UserController.cs
│  └─ AdminController.cs
│
├─ DTOs/                     # Data Transfer Objects + walidacje
│  ├─ AssetDto.cs
│  ├─ PortfolioDto.cs
│  ├─ TransactionDto.cs
│  ├─ UserDto.cs
│  └─ … (request/response DTOs)
│
├─ Mappers/                  # Mapowanie Model ↔ DTO
│  ├─ AssetMapper.cs
│  ├─ PortfolioMapper.cs
│  ├─ TransactionMapper.cs
│  └─ UserMapper.cs
│
├─ Models/                   # Encje domenowe
│  ├─ Asset.cs
│  ├─ Portfolio.cs
│  ├─ Transaction.cs
│  └─ User.cs
│
├─ Services/                 # Interfejsy + implementacje
│  ├─ IAssetService.cs
│  ├─ AssetService.cs
│  ├─ IPortfolioService.cs
│  ├─ PortfolioService.cs
│  ├─ ITransactionService.cs
│  ├─ TransactionService.cs
│  └─ IUserService.cs
│
├─ Data/
│  ├─ ApplicationDbContext.cs  # DbContext i konfiguracja tabel
│  └─ Migrations/              # EF Core migracje
│
├─ wwwroot/                  # Statyczne pliki (upload obrazów)
│
├─ appsettings.json          # Konfiguracja połączenia do bazy
├─ Program.cs                # Rejestracja usług, middleware
└─ MyWallet.csproj           # Plik projektu
```

---

## ⚙️ Uruchomienie

1. **Clone repozytorium**

   ```bash
   git clone https://github.com/Veetmatle/MyWallet.git
   cd MyWallet
   ```

2. **Przywróć pakiety NuGet**

   ```bash
   dotnet restore
   ```

3. **Zastosuj migracje i utwórz bazę**

   ```bash
   dotnet ef database update
   ```

4. **Uruchom aplikację**

   ```bash
   dotnet run
   ```

   Domyślnie nasłuchuje na `http://localhost:5210`.

---

## 📬 Kontrolery i endpointy

Każdy kontroler odpowiada za jedną grupę funkcji:

* **`/api/asset`**
  CRUD dla aktywów + wyszukiwanie cen i historii, upload obrazka.

* **`/api/portfolio`**
  Zarządzanie portfelami: CRUD, wartość, podsumowanie zysku/straty, historia, wykres.

* **`/api/transaction`**
  CRUD dla transakcji finansowych + raporty, obliczenia inwestycji/wycofań.

* **`/api/user`**
  Rejestracja, logowanie, pobranie bieżącego użytkownika, logout.

* **`/api/admin`**
  Operacje administracyjne (lista użytkowników, nadawanie/odbieranie roli Admin).

---

## 🛠 Warstwa usług (Services)

* **Interfejsy** (`IAssetService`, `IPortfolioService`, …) definiują kontrakty.
* **Implementacje** wykonują rzeczywistą logikę: komunikację z EF Core, zewnętrznymi API (np. giełdowymi), obliczenia finansowe.

---

## 🔄 DTO i mapowanie

* Każdy **DTO** zawiera tylko niezbędne pola i atrybuty walidacji (`[Required]`, `[Range]`, `[StringLength]`).
* **Mappery** (np. `AssetMapper`) przekształcają encję domenową na DTO i odwrotnie.

---

## 💾 Dostęp do danych (EF Core)

* **`ApplicationDbContext`**: wszystkie `DbSet<…>` i konfiguracja modeli.
* **Migracje** w folderze `Migrations/` – trzymają historię zmian w schemacie bazy.

---

# `Analiza indeksów do optymalizacji zapytań`

![image](https://github.com/user-attachments/assets/5414151e-e3e8-466a-ac2e-515b4290c50c)

#### Bez użycia indeksów:

![image](https://github.com/user-attachments/assets/2941960e-5b06-4f4e-8e75-5d4e3f932ba9)

#### Z użyciem indeksów:

![image](https://github.com/user-attachments/assets/b3408b65-4e6e-4ee0-98c3-ba969741b15d)


### Wnioski: 

Na pierwszym zrzucie (Hash Join + 2×Seq Scan) widać, że planner przeskanował obie tabele sekwencyjnie i zrobił Hash Join, a w Buffers miał tylko po 1 odczycie z każdej. To jest brak wykorzystania indeksów.

Na drugim zrzucie (Nested Loop + 2×Index Scan) widać, że:
  * Dla tabeli Assets użył się PK_Assets (indeks na kolumnie Id) i dopiero potem zastosował filtr Category = 'cryptocurrency'.
  * Dla Transactions użyło się idx_transactions_assetid (indeks na AssetId).
  * W Buffers: shared hit odpowiednio 2 odczyty dla Assets i 19 dla Transactions (razem 21, bo ciut więcej stron dojechało przez indeks),
  * Execution Time spadło prawie o połowę: z ~0.108 ms do ~0.052 ms,
  * Planning Time minimalnie wzrosło (bo planner rozważył więcej opcji).

**To pokazuje, że:**
* Plan domyślny (Seq Scan + Hash Join) omija indeksy, bo są one na małych zestawach „za drogie”, a tabele w naszym projekcie rozmiarem nie grzeszą.
* Plan zmuszony (musieliśmy użyć zakazu seq scan, bo nie chciał puścić - Nested Loop + Index Scan) faktycznie sięga po założone indeksy i – nawet przy tak małej próbce – daje zauważalny wzrost wydajności (krótszy czas wykonania).


### `Kilka przykładowych testów z postmana.`

![image](https://github.com/user-attachments/assets/071f2e88-a1bb-461a-849b-c05ec3e00ad1)

![image](https://github.com/user-attachments/assets/be12f368-2ff6-4804-86f8-05ebb055a4fe)

![image](https://github.com/user-attachments/assets/1c589fa7-6d07-4034-a332-5769d6f1806a)

![image](https://github.com/user-attachments/assets/b0e52680-7d82-48e2-b833-9e2714fd0661)

![image](https://github.com/user-attachments/assets/c2678fa4-5e6b-482e-b34f-54619830c63d)

![image](https://github.com/user-attachments/assets/3dec744a-ab1d-4682-b916-d8de51958cd2)

![image](https://github.com/user-attachments/assets/33c7b2b6-f6da-4365-be46-f7f4dd914a67)

![image](https://github.com/user-attachments/assets/ec06bcdc-8e17-479b-a998-afbc39cf0c21)

## 🚀 CI/CD z użyciem GitHub Actions

W projekcie skonfigurowano workflow, który przy każdym pushu do gałęzi `lask_branch` oraz przy każdym pull requeście automatycznie:
1. przywraca zależności (`dotnet restore`),  
2. buduje rozwiązanie w trybie Release (`dotnet build`),  
3. uruchamia testy jednostkowe (`dotnet test`).

![image](https://github.com/user-attachments/assets/4acb1848-3587-4dda-901d-4288f2105ae0)

### ** EF Core Logging **

Aby zobaczyć dokładne zapytania SQL wysyłane przez EF Core, włączamy logowanie w `Program.cs`:

![image](https://github.com/user-attachments/assets/461ab345-2812-451b-9ede-13da0e17986e)

Wywołujemy endpoint:

![image](https://github.com/user-attachments/assets/5edb5ddc-d9d2-47bc-ac9d-bc87c418e643)

W konsoli wypisuje się:

![image](https://github.com/user-attachments/assets/12cc96dc-ea25-4233-a6d1-0cd056c7f6c0)

SELECT … FROM "Portfolios" – pobierane są wszystkie portfele.
WHERE p."UserId" = @__userId_0 – wynik filtrowany do portfeli użytkownika o ID = 1.
@__userId_0 – parametr EF Core, zabezpieczający przed SQL Injection.

Zapytanie jest w pełni sparametryzowane, co zwiększa bezpieczeństwo i pozwala na ponowne użycie planu wykonania.
Dzięki EnableSensitiveDataLogging() w logach widać wartość parametru (@__userId_0 = '1') oraz czas wykonania (~2 ms).

#  **NBomber - zastosowanie**

* W projekcie przetestowano test obciążeniowy (load test, bombienie endpointa) dla API endpoint'a portfolio użytkownika.
* Kod wykonuje symulację obciążenia na endpoint http://localhost:5210/api/portfolio/user/4, wysyłając 10 żądań HTTP GET na sekundę przez 30 sekund (łącznie 300 żądań). (Dla odpalenia samego kodu bez frontu)
* Kod obsługuje trzy typy wyjątków:
  - HttpRequestException - problemy sieciowe (brak połączenia, DNS)

  - TaskCanceledException - timeout żądania (przekroczenie 30 sekund)

  - Exception - inne nieoczekiwane błędy


![image](https://github.com/user-attachments/assets/bb3f89df-427d-4f82-a153-d0c5d251625b)

![image](https://github.com/user-attachments/assets/b92c983f-f3ba-4f45-a68b-e913d5960236)

![image](https://github.com/user-attachments/assets/7cdbe31a-815a-4bb3-8c5d-24fcb19a46e4)

![image](https://github.com/user-attachments/assets/d1ba8bbe-900e-4368-b1a2-45fdf0f70f46)

### **Analiza wyników testów: **

* Wyniki testu obciążeniowego pokazują doskonałą wydajność API portfolio - wszystkie 300 żądań zakończyły się sukcesem (100% success rate) bez żadnych błędów. 
* Średni czas odpowiedzi wynosi zaledwie 1.32ms, a nawet w 99. percentylu (p99) odpowiedzi nie przekraczają 3.67ms, co świadczy o bardzo szybkiej i stabilnej responsywności systemu.
* API osiągnęło przepustowość 10 żądań na sekundę zgodnie z konfiguracją testu, co potwierdza, że system bez problemu radzi sobie z tym poziomem obciążenia





### `Kilka przykładowych ss z działania aplikacji`

![image](https://github.com/user-attachments/assets/9c4dcdae-7aee-4b1a-9c42-0864aaa574e7)

![image](https://github.com/user-attachments/assets/7a7a1334-aff8-4db4-adc6-6e50f964cfa4)

![image](https://github.com/user-attachments/assets/ca015125-ce5e-4e1d-9e80-662cfaca9394)

![image](https://github.com/user-attachments/assets/436f2a6a-ab8f-4c63-b6f8-b7f15f56e3de)

![image](https://github.com/user-attachments/assets/179b4922-1925-4f8c-b931-7edc6d810b02)

![image](https://github.com/user-attachments/assets/5bf17e2f-d3b7-4736-bc7f-2c81eefa16cc)

![image](https://github.com/user-attachments/assets/f38cc8e0-7627-464d-b255-ad73587cb2b2)

![image](https://github.com/user-attachments/assets/85188ffd-05fb-4cba-9159-301536243288)

![image](https://github.com/user-attachments/assets/248ce00a-f40d-4690-983d-1966f2d11b2e)

![image](https://github.com/user-attachments/assets/8696d4dd-4454-4ccc-9c3c-7751d72e223d)

![image](https://github.com/user-attachments/assets/e9ffbbb0-2714-49f8-91c0-c75742f7cf85)

![image](https://github.com/user-attachments/assets/38ebd167-7b1b-4e27-98e9-59be1b3b9106)




