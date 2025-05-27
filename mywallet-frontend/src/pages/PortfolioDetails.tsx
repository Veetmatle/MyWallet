// src/pages/PortfolioDetails.tsx
import { useEffect, useState, useRef, useMemo } from "react";
import { useParams } from "react-router-dom";
import "./dashboard.css";
import { useAssetHints, AssetHintDto } from "../hooks/useAssetHints";

interface AssetDto {
    id: number;
    symbol: string;
    name: string;
    category: string;
    averagePurchasePrice: number;
    currentPrice: number;
    quantity: number;
    currentValue: number;
    investedAmount: number;
    imagePath?: string;
}

interface PortfolioDto {
    id: number;
    name: string;
    description?: string;
    createdAt: string;
}

const ASSET_CATEGORIES = [
    { label: "Akcje", value: "stock" },
    { label: "ETF", value: "etf" },
    { label: "Kryptowaluty", value: "cryptocurrency" },
];

export default function PortfolioDetails() {
    const { id } = useParams<{ id: string }>();
    const [portfolio, setPortfolio] = useState<PortfolioDto | null>(null);
    const [assets, setAssets] = useState<AssetDto[]>([]);
    const [error, setError] = useState("");
    const { hints, fetchHints, clearHints } = useAssetHints();
    const debounceRef = useRef<number>(0);

    const [newAsset, setNewAsset] = useState({
        symbol: "",
        name: "",
        category: ASSET_CATEGORIES[0].value,
        currentPrice: "",
        quantity: "",
    });

    const [formErrors, setFormErrors] = useState<{ [key: string]: string }>({});

    useEffect(() => {
        if (!id) return;

        fetch(`/api/portfolio/${id}`)
            .then((r) => r.json())
            .then(setPortfolio)
            .catch(() => setError("Nie udało się pobrać portfela."));

        fetch(`/api/asset/portfolio/${id}`)
            .then((r) => r.json())
            .then((data: any[]) => {
                const mapped: AssetDto[] = data.map((a) => ({
                    id: a.id,
                    symbol: a.symbol,
                    name: a.name,
                    category: a.category,
                    averagePurchasePrice: a.averagePurchasePrice,
                    currentPrice: a.currentPrice,
                    quantity: a.quantity,
                    currentValue: a.currentPrice * a.quantity,
                    investedAmount: a.investedAmount,
                    imagePath: a.imagePath,
                }));
                setAssets(mapped);
            })
            .catch(() => setError("Nie udało się pobrać aktywów."));
    }, [id]);

    const cryptoSymbols = useMemo(
        () => assets.filter((a) => a.category === "cryptocurrency").map((a) => a.symbol),
        [assets]
    );

    useEffect(() => {
        if (cryptoSymbols.length === 0) return;

        const updateAll = async () => {
            try {
                const ids = cryptoSymbols.join(",");
                const res = await fetch(
                    `https://api.coingecko.com/api/v3/simple/price?ids=${ids}&vs_currencies=usd`
                );
                if (!res.ok) throw new Error();

                const data: Record<string, { usd: number }> = await res.json();
                setAssets((prev) =>
                    prev.map((a) => {
                        if (a.category === "cryptocurrency" && data[a.symbol]) {
                            const price = data[a.symbol].usd;
                            return {
                                ...a,
                                currentPrice: price,
                                currentValue: price * a.quantity,
                            };
                        }
                        return a;
                    })
                );
            } catch {
                console.warn("Błąd grupowego odświeżania cen krypto");
            }
        };

        updateAll();
        const timer = setInterval(updateAll, 5 * 60 * 1000);
        return () => clearInterval(timer);
    }, [cryptoSymbols]);

    const handleSymbolChange = (value: string) => {
        setNewAsset((prev) => ({ ...prev, symbol: value }));

        if (debounceRef.current) {
            clearTimeout(debounceRef.current);
        }

        if (value.length >= 2) {
            debounceRef.current = window.setTimeout(() => {
                fetchHints(newAsset.category, value);
            }, 300);
        } else {
            clearHints();
        }
    };

    const handleSelectHint = async (hint: AssetHintDto) => {
        const category = newAsset.category;
        setNewAsset((p) => ({ ...p, symbol: hint.symbol, name: hint.name }));
        clearHints();
        try {
            const res = await fetch(
                `/api/asset/price?category=${encodeURIComponent(
                    category
                )}&symbol=${encodeURIComponent(hint.symbol)}`
            );
            if (!res.ok) throw new Error();
            const price: number = await res.json();
            setNewAsset((p) => ({ ...p, currentPrice: price.toString() }));
        } catch {
            console.error("Błąd pobierania ceny podpowiedzi");
        }
    };

    const validateForm = () => {
        const errors: { [key: string]: string } = {};

        if (!newAsset.symbol.trim()) {
            errors.symbol = "Symbol jest wymagany.";
        }

        if (!newAsset.name.trim()) {
            errors.name = "Nazwa jest wymagana.";
        }

        const price = parseFloat(newAsset.currentPrice);
        if (!newAsset.currentPrice || isNaN(price) || price <= 0) {
            errors.currentPrice = "Cena musi być liczbą większą od 0.";
        }

        const quantity = parseFloat(newAsset.quantity);
        if (!newAsset.quantity || isNaN(quantity) || quantity <= 0) {
            errors.quantity = "Ilość musi być liczbą większą od 0.";
        }

        setFormErrors(errors);
        return Object.keys(errors).length === 0;
    };

    const handleAddAsset = async () => {
        setError("");
        setFormErrors({});

        if (!validateForm()) {
            return;
        }

        const portfolioId = Number(id);
        if (isNaN(portfolioId)) {
            setError("Nieprawidłowe ID portfela.");
            return;
        }

        const purchase = parseFloat(newAsset.currentPrice);
        const qty = parseFloat(newAsset.quantity);

        const payload = {
            symbol: newAsset.symbol.trim(),
            name: newAsset.name.trim(),
            category: newAsset.category,
            currentPrice: purchase,
            quantity: qty,
            portfolioId,
        };

        try {
            const res = await fetch("/api/asset", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload),
            });

            if (res.status === 400) {
                const data = await res.json();
                if (data.errors) {
                    const backendErrors: { [key: string]: string } = {};
                    for (const key in data.errors) {
                        backendErrors[key.toLowerCase()] = Array.isArray(data.errors[key])
                            ? data.errors[key][0]
                            : data.errors[key];
                    }
                    setFormErrors(backendErrors);
                } else {
                    setError(data.message || "Błąd walidacji danych.");
                }
                return;
            }

            if (!res.ok) {
                const errorText = await res.text();
                throw new Error(errorText || "Błąd podczas dodawania aktywa.");
            }

            const a: any = await res.json();
            const added: AssetDto = {
                id: a.id,
                symbol: a.symbol,
                name: a.name,
                category: a.category,
                averagePurchasePrice: a.averagePurchasePrice,
                currentPrice: a.currentPrice,
                quantity: a.quantity,
                currentValue: a.currentPrice * a.quantity,
                investedAmount: a.investedAmount,
                imagePath: a.imagePath,
            };

            setAssets((prev) => {
                const existingIndex = prev.findIndex(asset =>
                    asset.symbol.toLowerCase() === added.symbol.toLowerCase() &&
                    asset.category === added.category
                );
                if (existingIndex >= 0) {
                    const updated = [...prev];
                    updated[existingIndex] = added;
                    return updated;
                } else {
                    return [...prev, added];
                }
            });

            setNewAsset({
                symbol: "",
                name: "",
                category: newAsset.category,
                currentPrice: "",
                quantity: "",
            });
            setFormErrors({});
            clearHints();
        } catch (err: any) {
            setError(err.message || "Błąd podczas dodawania aktywa.");
        }
    };

    const handleDelete = async (assetId: number) => {
        if (!window.confirm("Czy na pewno chcesz usunąć to aktywo?")) return;
        try {
            const res = await fetch(`/api/asset/${assetId}`, { method: "DELETE" });
            if (!res.ok) throw new Error("Nie udało się usunąć aktywa.");
            setAssets((prev) => prev.filter((a) => a.id !== assetId));
        } catch (err: any) {
            alert(err.message || "Nie udało się usunąć aktywa.");
        }
    };

    const handleRefresh = async (assetId: number) => {
        const asset = assets.find((a) => a.id === assetId);
        if (!asset) return;
        try {
            const res = await fetch(
                `/api/asset/price?category=${encodeURIComponent(
                    asset.category
                )}&symbol=${encodeURIComponent(asset.symbol)}`
            );
            if (!res.ok) throw new Error("Nie udało się pobrać ceny.");

            const price: number = await res.json();
            setAssets((prev) =>
                prev.map((x) =>
                    x.id === assetId
                        ? { ...x, currentPrice: price, currentValue: price * x.quantity }
                        : x
                )
            );
        } catch (err: any) {
            alert(err.message || "Nie udało się odświeżyć ceny.");
        }
    };

    const { totalValue, totalInvested, profitPercentage, profitAmount } = useMemo(() => {
        const totalValue = assets.reduce((sum, a) => sum + a.currentValue, 0);
        const totalInvested = assets.reduce((sum, a) => sum + (a.averagePurchasePrice * a.quantity), 0);
        const profitAmount = totalValue - totalInvested;
        const profitPercentage = totalInvested > 0
            ? (profitAmount / totalInvested) * 100
            : 0;

        return {
            totalValue: totalValue.toFixed(2),
            totalInvested: totalInvested.toFixed(2),
            profitPercentage: profitPercentage.toFixed(1),
            profitAmount: profitAmount.toFixed(2)
        };
    }, [assets]);

    const computeTotal = () => {
        const purchase = parseFloat(newAsset.currentPrice) || 0;
        const qty = parseFloat(newAsset.quantity) || 0;
        return (purchase * qty).toFixed(2);
    };

    useEffect(() => {
        return () => {
            if (debounceRef.current) {
                clearTimeout(debounceRef.current);
            }
        };
    }, []);

    if (!portfolio) return <div className="dashboard-content"><p>Ładowanie…</p></div>;

    return (
        <div className="dashboard-content">
            <div className="portfolio-header">
                <h2>{portfolio.name}</h2>
                <div className="portfolio-value-container">
                    <div className="portfolio-value">Wartość portfela: ${totalValue}</div>
                    <div className={`profit-loss ${parseFloat(profitAmount) >= 0 ? 'profit' : 'loss'}`}>
                        {parseFloat(profitAmount) >= 0 ? '+' : ''}{profitAmount}$ ({profitPercentage}%)
                    </div>
                </div>
            </div>

            {portfolio.description && <p>{portfolio.description}</p>}
            <hr />

            <h3>Aktywa</h3>
            {assets.length === 0 ? (
                <p>Brak aktywów w portfelu.</p>
            ) : (
                <div className="portfolios-list">
                    {assets.map((a) => (
                        <div key={a.id} className="portfolio-card">
                            <button className="refresh-btn" onClick={() => handleRefresh(a.id)} title="Odśwież cenę">⟳</button>
                            <button className="delete-btn" onClick={() => handleDelete(a.id)} title="Usuń aktywo">×</button>

                            <h4>
                                {a.symbol.toUpperCase()} — {a.name}
                            </h4>

                            <p>Kategoria: {ASSET_CATEGORIES.find(cat => cat.value === a.category)?.label || a.category}</p>
                            <p>Śr. cena zakupu: ${a.averagePurchasePrice.toFixed(2)}</p>
                            <p>Cena aktualna: ${a.currentPrice.toFixed(2)}</p>
                            <p>Ilość: {a.quantity}</p>
                            <p>
                                <strong>Wartość: ${a.currentValue.toFixed(2)}</strong>
                                {a.currentPrice !== a.averagePurchasePrice && (
                                    <span style={{
                                        color: a.currentPrice > a.averagePurchasePrice ? '#16a34a' : '#dc2626',
                                        fontSize: '0.8em',
                                        marginLeft: '8px'
                                    }}>
                                        ({a.currentPrice > a.averagePurchasePrice ? '+' : ''}
                                        {(((a.currentPrice - a.averagePurchasePrice) / a.averagePurchasePrice) * 100).toFixed(1)}%)
                                    </span>
                                )}
                            </p>
                        </div>
                    ))}
                </div>
            )}

            <hr />

            <h3>Dodaj aktywo</h3>
            <div className="portfolio-form">
                <div>
                    <label>Kategoria:</label>
                    <select
                        value={newAsset.category}
                        onChange={(e) => {
                            setNewAsset(prev => ({
                                ...prev,
                                category: e.target.value,
                                symbol: "",
                                name: "",
                                currentPrice: ""
                            }));
                            clearHints();
                        }}
                    >
                        {ASSET_CATEGORIES.map((cat) => (
                            <option key={cat.value} value={cat.value}>
                                {cat.label}
                            </option>
                        ))}
                    </select>
                    {formErrors.category && <div className="error-message">{formErrors.category}</div>}
                </div>

                <div style={{ position: "relative" }}>
                    <label>Symbol:</label>
                    <input
                        type="text"
                        placeholder="np. AAPL, bitcoin"
                        value={newAsset.symbol}
                        onChange={(e) => handleSymbolChange(e.target.value)}
                    />
                    {formErrors.symbol && <div className="error-message">{formErrors.symbol}</div>}

                    {hints.length > 0 && (
                        <ul className="hints-list">
                            {hints.map((hint, i) => (
                                <li key={i} onClick={() => handleSelectHint(hint)}>
                                    <strong>{hint.symbol}</strong> — {hint.name}
                                </li>
                            ))}
                        </ul>
                    )}
                </div>

                <div>
                    <label>Nazwa:</label>
                    <input
                        type="text"
                        placeholder="Pełna nazwa"
                        value={newAsset.name}
                        onChange={(e) => setNewAsset(prev => ({ ...prev, name: e.target.value }))}
                    />
                    {formErrors.name && <div className="error-message">{formErrors.name}</div>}
                </div>

                <div>
                    <label>Cena zakupu ($):</label>
                    <input
                        type="number"
                        step="0.01"
                        min="0"
                        placeholder="0.00"
                        value={newAsset.currentPrice}
                        onChange={(e) => setNewAsset(prev => ({ ...prev, currentPrice: e.target.value }))}
                    />
                    {formErrors.currentPrice && <div className="error-message">{formErrors.currentPrice}</div>}
                </div>

                <div>
                    <label>Ilość:</label>
                    <input
                        type="number"
                        step="0.000001"
                        min="0"
                        placeholder="0"
                        value={newAsset.quantity}
                        onChange={(e) => setNewAsset(prev => ({ ...prev, quantity: e.target.value }))}
                    />
                    {formErrors.quantity && <div className="error-message">{formErrors.quantity}</div>}
                </div>

                {newAsset.currentPrice && newAsset.quantity && (
                    <div style={{ gridColumn: "span 2", padding: "10px", backgroundColor: "#f8f9fa", borderRadius: "4px" }}>
                        <strong>Łączna wartość: ${computeTotal()}</strong>
                    </div>
                )}

                <button className="save-btn" onClick={handleAddAsset}>
                    Dodaj aktywo
                </button>

                {error && <div className="error-message" style={{ gridColumn: "span 2" }}>{error}</div>}
            </div>
        </div>
    );
}