import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import "./dashboard.css";

interface AssetSuggestion {
    symbol: string;
    name: string;
}

interface AssetDto {
    id: number;
    symbol: string;
    name: string;
    category: string;
    initialPrice: number;
    currentPrice: number;
    quantity: number;
}

interface PortfolioDto {
    id: number;
    name: string;
    description?: string;
    createdAt: string;
}

export default function PortfolioDetails() {
    const { id } = useParams();
    const navigate = useNavigate();

    const [portfolio, setPortfolio] = useState<PortfolioDto | null>(null);
    const [assets, setAssets] = useState<AssetDto[]>([]);
    const [error, setError] = useState("");

    const [showAssetForm, setShowAssetForm] = useState(false);
    const [symbolQuery, setSymbolQuery] = useState("");
    const [symbolResults, setSymbolResults] = useState<AssetSuggestion[]>([]);
    const [selectedSymbol, setSelectedSymbol] = useState("");
    const [selectedName, setSelectedName] = useState("");
    const [selectedCategory, setSelectedCategory] = useState("crypto");
    const [purchasePrice, setPurchasePrice] = useState("");
    const [quantity, setQuantity] = useState("");
    const [currentPrice, setCurrentPrice] = useState<number | null>(null);

    const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
    const [deletePassword, setDeletePassword] = useState("");

    useEffect(() => {
        if (!id) return;

        fetch(`/api/portfolio/${id}`)
            .then((res) => res.json())
            .then(setPortfolio)
            .catch(() => setError("Nie udało się pobrać portfela."));

        fetch(`/api/asset/portfolio/${id}`)
            .then((res) => res.json())
            .then(setAssets)
            .catch(() => setError("Nie udało się pobrać aktywów."));
    }, [id]);

    useEffect(() => {
        const delay = setTimeout(() => {
            if (symbolQuery.length < 2) return;

            fetch(`/api/external/search?query=${symbolQuery}&category=${selectedCategory}`)
                .then((res) => res.json())
                .then(setSymbolResults)
                .catch(() => setSymbolResults([]));
        }, 300);

        return () => clearTimeout(delay);
    }, [symbolQuery, selectedCategory]);

    const handleSelectSymbol = async (symbol: string, name: string) => {
        setSelectedSymbol(symbol);
        setSelectedName(name);
        setSymbolQuery(symbol);
        setSymbolResults([]);
        try {
            const res = await fetch(`/api/external/current-price?symbol=${symbol}&category=${selectedCategory}`);
            const data = await res.json();
            setCurrentPrice(data.currentPrice);
        } catch {
            setCurrentPrice(null);
        }
    };

    const handleAddAsset = async () => {
        if (!selectedSymbol || !selectedName || !purchasePrice || !quantity) {
            setError("Uzupełnij wszystkie pola.");
            return;
        }

        const payload = {
            symbol: selectedSymbol,
            name: selectedName,
            category: selectedCategory,
            initialPrice: parseFloat(purchasePrice),
            currentPrice: currentPrice ?? parseFloat(purchasePrice),
            quantity: parseFloat(quantity),
            portfolioId: parseInt(id || "")
        };

        try {
            const res = await fetch("/api/asset", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            if (!res.ok) {
                const data = await res.json();
                const msg = data?.errors ? Object.values(data.errors).flat().join(" ") : "Błąd dodawania aktywa.";
                setError(msg);
                return;
            }

            const added = await res.json();
            setAssets([...assets, added]);
            setSelectedSymbol("");
            setSelectedName("");
            setSymbolQuery("");
            setQuantity("");
            setPurchasePrice("");
            setCurrentPrice(null);
            setError("");
            setShowAssetForm(false);
        } catch {
            setError("Nie udało się dodać aktywa.");
        }
    };

    const handleDeletePortfolio = async () => {
        if (deletePassword.trim().length < 3) {
            setError("Podaj hasło, aby potwierdzić.");
            return;
        }

        const confirmed = window.confirm("Czy na pewno chcesz usunąć ten portfel?");
        if (!confirmed) return;

        try {
            const res = await fetch(`/api/portfolio/${id}`, { method: "DELETE" });
            if (!res.ok) throw new Error();
            navigate("/dashboard");
        } catch {
            setError("Nie udało się usunąć portfela.");
        }
    };

    if (!portfolio) return <p>Ładowanie...</p>;

    return (
        <div className="dashboard-content">
            <section style={{ marginBottom: "40px" }}>
                <h2>{portfolio.name}</h2>
                <p>{portfolio.description}</p>
            </section>

            <section style={{ marginBottom: "40px" }}>
                <h3>Aktywa</h3>
                {assets.length === 0 ? (
                    <p>Brak aktywów.</p>
                ) : (
                    <div className="portfolios-list">
                        {assets.map((a) => (
                            <div key={a.id} className="portfolio-card">
                                <h4>{a.symbol} – {a.name}</h4>
                                <p>Kategoria: {a.category}</p>
                                <p>Ilość: {a.quantity}</p>
                                <p>Cena zakupu: {a.initialPrice.toFixed(2)}</p>
                                <p>Aktualna cena: {a.currentPrice.toFixed(2)}</p>
                            </div>
                        ))}
                    </div>
                )}
            </section>

            <section style={{ marginBottom: "40px" }}>
                <button className="save-btn" onClick={() => setShowAssetForm(!showAssetForm)}>
                    {showAssetForm ? "Anuluj" : "Dodaj aktywo"}
                </button>

                {showAssetForm && (
                    <div className="portfolio-form" style={{ marginTop: "20px" }}>
                        <select value={selectedCategory} onChange={(e) => setSelectedCategory(e.target.value)}>
                            <option value="crypto">Kryptowaluta</option>
                            <option value="stock">Akcja</option>
                            <option value="etf">ETF</option>
                            <option value="commodity">Surowiec</option>
                        </select>

                        <div className="symbol-search">
                            <input
                                type="text"
                                placeholder="Wpisz min. 2 litery, np. BTC"
                                value={symbolQuery}
                                onChange={(e) => setSymbolQuery(e.target.value)}
                            />
                            {symbolQuery.length >= 2 && symbolResults.length === 0 && (
                                <div className="no-results">Brak wyników</div>
                            )}
                            {symbolResults.length > 0 && (
                                <ul className="symbol-dropdown">
                                    {symbolResults.map((res) => (
                                        <li key={res.symbol} onClick={() => handleSelectSymbol(res.symbol, res.name)}>
                                            <strong>{res.symbol}</strong> – {res.name}
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </div>

                        <input
                            type="number"
                            placeholder="Cena zakupu"
                            value={purchasePrice}
                            onChange={(e) => setPurchasePrice(e.target.value)}
                        />

                        <input
                            type="number"
                            placeholder="Ilość"
                            value={quantity}
                            onChange={(e) => setQuantity(e.target.value)}
                        />

                        {currentPrice && <small>Aktualna cena: {currentPrice.toFixed(2)}</small>}

                        <button className="save-btn" onClick={handleAddAsset}>Zapisz aktywo</button>
                    </div>
                )}
            </section>

            <section>
                <button className="save-btn" onClick={() => setShowDeleteConfirm(!showDeleteConfirm)}>
                    {showDeleteConfirm ? "Anuluj" : "Usuń portfel"}
                </button>

                {showDeleteConfirm && (
                    <div className="portfolio-form" style={{ marginTop: "20px" }}>
                        <input
                            type="password"
                            placeholder="Wpisz hasło"
                            value={deletePassword}
                            onChange={(e) => setDeletePassword(e.target.value)}
                        />
                        <button className="save-btn" onClick={handleDeletePortfolio}>Potwierdź usunięcie</button>
                    </div>
                )}
            </section>

            {error && <div className="error-message" style={{ marginTop: "20px" }}>{error}</div>}
        </div>
    );
}
