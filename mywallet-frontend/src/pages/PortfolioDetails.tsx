import { useEffect, useState, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import "./dashboard.css";
import { useAssetHints, AssetHintDto } from "../hooks/useAssetHints";

interface AssetDto {
    id: number;
    symbol: string;
    name: string;
    category: string;
    currentPrice: number;
    quantity: number;
    imagePath?: string;
}

interface PortfolioDto {
    id: number;
    name: string;
    description?: string;
    createdAt: string;
}

export default function PortfolioDetails() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const [portfolio, setPortfolio] = useState<PortfolioDto | null>(null);
    const [assets, setAssets] = useState<AssetDto[]>([]);
    const [error, setError] = useState("");

    const { hints, fetchHints, clearHints } = useAssetHints();
    const debounceRef = useRef<number>(0);

    const [newAsset, setNewAsset] = useState({
        symbol: "",
        name: "",
        category: "stock",
        currentPrice: "",
        quantity: "",
    });

    // 1) Pobierz portfel i jego aktywa
    useEffect(() => {
        if (!id) return;

        fetch(`http://localhost:5210/api/portfolio/${id}`)
            .then((r) => r.json())
            .then(setPortfolio)
            .catch(() => setError("Nie udało się pobrać portfela."));

        fetch(`http://localhost:5210/api/asset/portfolio/${id}`)
            .then((r) => r.json())
            .then(setAssets)
            .catch(() => setError("Nie udało się pobrać aktywów."));
    }, [id]);

    // 2) Kliknięcie na podpowiedź → uzupełnij pola i wyczyść listę
    const handleSelectHint = (hint: AssetHintDto) => {
        setNewAsset((prev) => ({
            ...prev,
            symbol: hint.symbol,
            name: hint.name,
        }));
        clearHints();
    };

    // 3) Dodawanie aktywa
    const handleAddAsset = async () => {
        setError("");
        const portfolioId = Number(id);
        if (isNaN(portfolioId)) {
            setError("Nieprawidłowe ID portfela.");
            return;
        }
        

        const payload = {
            symbol: newAsset.symbol,
            name: newAsset.name,
            category: newAsset.category,
            currentPrice: parseFloat(newAsset.currentPrice),
            quantity: parseFloat(newAsset.quantity),
            portfolioId,
        };

        try {
            const res = await fetch("http://localhost:5210/api/asset", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload),
            });

            if (res.status === 400) {
                const data = await res.json();
                setError(Object.values(data.errors).flat().join(" "));
                return;
            }
            if (!res.ok) throw new Error();

            const added: AssetDto = await res.json();
            setAssets((prev) => [...prev, added]);
            setNewAsset({
                symbol: "",
                name: "",
                category: "stock",
                currentPrice: "",
                quantity: "",
            });
        } catch {
            setError("Błąd podczas dodawania aktywa.");
        }
    };

    if (!portfolio) return <p>Ładowanie…</p>;

    return (
        <div className="dashboard-content">
            <h2>{portfolio.name}</h2>
            <p>{portfolio.description}</p>

            <hr />
            <h3>Dodaj aktywo</h3>
            <div className="portfolio-form">
                {/* SYMBOL + lista podpowiedzi */}
                <div style={{ position: "relative" }}>
                    <input
                        placeholder="Symbol"
                        value={newAsset.symbol}
                        onChange={(e) => {
                            const val = e.target.value.trim().toLowerCase();
                            setNewAsset((prev) => ({ ...prev, symbol: val }));

                            // debounce fetchHints
                            clearTimeout(debounceRef.current);
                            if (val.length >= 2 && newAsset.category) {
                                debounceRef.current = window.setTimeout(() => {
                                    fetchHints(val, newAsset.category);
                                }, 300);
                            }
                        }}
                        style={{ width: "100%" }}
                    />
                    {hints.length > 0 && (
                        <ul
                            className="hints-list"
                            style={{
                                position: "absolute",
                                top: "100%",
                                left: 0,
                                right: 0,
                                background: "white",
                                border: "1px solid #ccc",
                                maxHeight: 200,
                                overflowY: "auto",
                                margin: 0,
                                padding: 0,
                                listStyle: "none",
                                zIndex: 10,
                            }}
                        >
                            {hints.map((hint) => (
                                <li
                                    key={hint.symbol}
                                    onClick={() => handleSelectHint(hint)}
                                    style={{ padding: "8px", cursor: "pointer" }}
                                >
                                    {hint.symbol} – {hint.name}
                                </li>
                            ))}
                        </ul>
                    )}
                </div>

                {/* NAZWA */}
                <input
                    placeholder="Nazwa"
                    value={newAsset.name}
                    onChange={(e) =>
                        setNewAsset((prev) => ({ ...prev, name: e.target.value }))
                    }
                />
                {/* KATEGORIA */}
                <input
                    placeholder="Kategoria (np. crypto, stock)"
                    value={newAsset.category}
                    onChange={(e) =>
                        setNewAsset((prev) => ({ ...prev, category: e.target.value }))
                    }
                />
                {/* CENA */}
                <input
                    placeholder="Aktualna cena"
                    type="number"
                    value={newAsset.currentPrice}
                    onChange={(e) =>
                        setNewAsset((prev) => ({
                            ...prev,
                            currentPrice: e.target.value,
                        }))
                    }
                />
                {/* ILOŚĆ */}
                <input
                    placeholder="Ilość"
                    type="number"
                    value={newAsset.quantity}
                    onChange={(e) =>
                        setNewAsset((prev) => ({ ...prev, quantity: e.target.value }))
                    }
                />

                <button className="save-btn" onClick={handleAddAsset}>
                    Dodaj aktywo
                </button>
                {error && <div className="error-message">{error}</div>}
            </div>

            <hr />
            <h3>Aktywa</h3>
            {assets.length === 0 ? (
                <p>Brak aktyw&oacute;w.</p>
            ) : (
                <div className="portfolios-list">
                    {assets.map((a) => (
                        <div key={a.id} className="portfolio-card">
                            <h4>
                                {a.symbol.toUpperCase()} – {a.name}
                            </h4>
                            <p>Kategoria: {a.category}</p>
                            <p>
                                Cena: {a.currentPrice.toFixed(2)} | Ilość: {a.quantity}
                            </p>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
