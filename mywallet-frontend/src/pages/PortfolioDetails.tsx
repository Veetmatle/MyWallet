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
    purchasePrice: number;    // cena zakupu za sztukę
    currentPrice: number;     // aktualna cena (live dla krypto)
    quantity: number;
    currentValue: number;     // quantity × currentPrice
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

    // 1) Pobierz portfel i aktywa
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
                    purchasePrice: a.currentPrice,
                    currentPrice: a.currentPrice,
                    quantity: a.quantity,
                    currentValue: a.currentPrice * a.quantity,
                    imagePath: a.imagePath,
                }));
                setAssets(mapped);
            })
            .catch(() => setError("Nie udało się pobrać aktywów."));
    }, [id]);

    // 2) Grupowane autoodświeżanie cen krypto co 5 minut
    const cryptoSymbols = useMemo(
        () =>
            assets
                .filter((a) => a.category === "cryptocurrency")
                .map((a) => a.symbol),
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

    // 3) Selektor podpowiedzi
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

    // 4) Dodawanie aktywa
    const handleAddAsset = async () => {
        setError("");
        const portfolioId = Number(id);
        if (isNaN(portfolioId)) {
            setError("Nieprawidłowe ID portfela.");
            return;
        }
        const purchase = parseFloat(newAsset.currentPrice) || 0;
        const qty = parseFloat(newAsset.quantity) || 0;

        const payload = {
            symbol: newAsset.symbol,
            name: newAsset.name,
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
                setError(Object.values(data.errors).flat().join(" "));
                return;
            }
            if (!res.ok) throw new Error();
            const a: any = await res.json();
            const added: AssetDto = {
                id: a.id,
                symbol: a.symbol,
                name: a.name,
                category: a.category,
                purchasePrice: a.currentPrice,
                currentPrice: a.currentPrice,
                quantity: a.quantity,
                currentValue: a.currentPrice * a.quantity,
                imagePath: a.imagePath,
            };
            setAssets((prev) => [...prev, added]);
            setNewAsset({
                symbol: "",
                name: "",
                category: newAsset.category,
                currentPrice: "",
                quantity: "",
            });
        } catch {
            setError("Błąd podczas dodawania aktywa.");
        }
    };

    // 5) Usuwanie
    const handleDelete = async (assetId: number) => {
        if (!window.confirm("Czy na pewno chcesz usunąć to aktywo?")) return;
        try {
            const res = await fetch(`/api/asset/${assetId}`, { method: "DELETE" });
            if (!res.ok) throw new Error();
            setAssets((prev) => prev.filter((a) => a.id !== assetId));
        } catch {
            alert("Nie udało się usunąć aktywa.");
        }
    };

    // 6) Ręczne odświeżenie pojedynczego
    const handleRefresh = async (assetId: number) => {
        const asset = assets.find((a) => a.id === assetId);
        if (!asset) return;
        try {
            const res = await fetch(
                `/api/asset/price?category=${encodeURIComponent(
                    asset.category
                )}&symbol=${encodeURIComponent(asset.symbol)}`
            );
            if (!res.ok) throw new Error();
            const price: number = await res.json();
            setAssets((prev) =>
                prev.map((x) =>
                    x.id === assetId
                        ? { ...x, currentPrice: price, currentValue: price * x.quantity }
                        : x
                )
            );
        } catch {
            alert("Nie udało się odświeżyć ceny.");
        }
    };

    // 7) Wartość portfela
    const totalValue = assets
        .reduce((sum, a) => sum + a.currentValue, 0)
        .toFixed(2);

    // 8) Wartość podglądu w formularzu
    const computeTotal = () => {
        const purchase = parseFloat(newAsset.currentPrice) || 0;
        const qty = parseFloat(newAsset.quantity) || 0;
        return (purchase * qty).toFixed(2);
    };

    if (!portfolio) return <p>Ładowanie…</p>;

    return (
        <div className="dashboard-content">
            <div className="portfolio-header">
                <h2>{portfolio.name}</h2>
                <div className="portfolio-value">Wartość portfela: {totalValue}</div>
            </div>
            <p>{portfolio.description}</p>
            <hr />

            <h3>Aktywa</h3>
            {assets.length === 0 ? (
                <p>Brak aktywów.</p>
            ) : (
                <div className="portfolios-list">
                    {assets.map((a) => (
                        <div key={a.id} className="portfolio-card">
                            <button
                                className="refresh-btn"
                                onClick={() => handleRefresh(a.id)}
                            >
                                ⟳
                            </button>
                            <button
                                className="delete-btn"
                                onClick={() => handleDelete(a.id)}
                            >
                                ×
                            </button>
                            <h4>
                                {a.symbol.toUpperCase()} — {a.name}
                            </h4>
                            <p>Kategoria: {a.category}</p>
                            <p>Cena zakupu: {a.purchasePrice.toFixed(2)}</p>
                            <p>Cena aktualna: {a.currentPrice.toFixed(2)}</p>
                            <p>Ilość: {a.quantity}</p>
                            <p>Wartość: {a.currentValue.toFixed(2)}</p>
                        </div>
                    ))}
                </div>
            )}

            <hr />
            <h3>Dodaj aktywo</h3>
            <div className="portfolio-form">
                {/* 1. Klasa aktywów */}
                <div>
                    <label htmlFor="category">Klasa aktywów</label>
                    <select
                        id="category"
                        value={newAsset.category}
                        onChange={(e) => {
                            setNewAsset((p) => ({
                                ...p,
                                category: e.target.value,
                                symbol: "",
                                name: "",
                                currentPrice: "",
                                quantity: "",
                            }));
                            clearHints();
                        }}
                    >
                        {ASSET_CATEGORIES.map((c) => (
                            <option key={c.value} value={c.value}>
                                {c.label}
                            </option>
                        ))}
                    </select>
                </div>

                {/* 2. Symbol + podpowiedzi */}
                <div style={{ position: "relative" }}>
                    <label htmlFor="symbol">Symbol</label>
                    <input
                        id="symbol"
                        placeholder="Symbol"
                        value={newAsset.symbol}
                        onChange={(e) => {
                            const val = e.target.value.trim().toLowerCase();
                            setNewAsset((p) => ({ ...p, symbol: val }));
                            clearTimeout(debounceRef.current);
                            if (val.length >= 2) {
                                debounceRef.current = window.setTimeout(
                                    () => fetchHints(val, newAsset.category),
                                    300
                                );
                            }
                        }}
                    />
                    {hints.length > 0 && (
                        <ul className="hints-list">
                            {hints.map((h) => (
                                <li key={h.symbol} onClick={() => handleSelectHint(h)}>
                                    <strong>{h.symbol}</strong> — {h.name}
                                </li>
                            ))}
                        </ul>
                    )}
                </div>

                {/* 3. Nazwa */}
                <div>
                    <label htmlFor="name">Nazwa</label>
                    <input
                        id="name"
                        placeholder="Nazwa"
                        value={newAsset.name}
                        onChange={(e) =>
                            setNewAsset((p) => ({ ...p, name: e.target.value }))
                        }
                    />
                </div>

                {/* 4. Cena zakupu */}
                <div>
                    <label htmlFor="price">Cena zakupu</label>
                    <input
                        id="price"
                        type="number"
                        placeholder="0.00"
                        value={newAsset.currentPrice}
                        onChange={(e) =>
                            setNewAsset((p) => ({ ...p, currentPrice: e.target.value }))
                        }
                    />
                </div>

                {/* 5. Ilość */}
                <div>
                    <label htmlFor="quantity">Ilość</label>
                    <input
                        id="quantity"
                        type="number"
                        placeholder="0"
                        value={newAsset.quantity}
                        onChange={(e) =>
                            setNewAsset((p) => ({ ...p, quantity: e.target.value }))
                        }
                    />
                </div>

                {/* 6. Wartość (tylko do odczytu) */}
                <div>
                    <label htmlFor="total">Wartość</label>
                    <input id="total" type="text" value={computeTotal()} disabled />
                </div>

                {/* 7. Dodaj aktywo */}
                <button className="save-btn" onClick={handleAddAsset}>
                    Dodaj aktywo
                </button>
                {error && <div className="error-message">{error}</div>}
            </div>
        </div>
    );
}
