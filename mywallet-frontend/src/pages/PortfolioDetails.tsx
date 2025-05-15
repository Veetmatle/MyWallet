import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import "./dashboard.css";

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
    const { id } = useParams();
    const navigate = useNavigate();
    const [portfolio, setPortfolio] = useState<PortfolioDto | null>(null);
    const [assets, setAssets] = useState<AssetDto[]>([]);
    const [error, setError] = useState("");

    const [editing, setEditing] = useState(false);
    const [editName, setEditName] = useState("");
    const [editDescription, setEditDescription] = useState("");

    const [newAsset, setNewAsset] = useState({
        symbol: "",
        name: "",
        category: "",
        currentPrice: "",
        quantity: "",
    });

    const [deleteConfirm, setDeleteConfirm] = useState(false);
    const [password, setPassword] = useState("");

    const user = JSON.parse(localStorage.getItem("user") || "{}");

    useEffect(() => {
        if (!id) return;
        fetch(`/api/portfolio/${id}`)
            .then((res) => res.json())
            .then((data) => setPortfolio(data))
            .catch(() => setError("Nie udało się pobrać portfela."));

        fetch(`/api/asset/portfolio/${id}`)
            .then((res) => res.json())
            .then((data) => setAssets(data))
            .catch(() => setError("Nie udało się pobrać aktywów."));
    }, [id]);

    const handleAddAsset = async () => {
        const payload = {
            symbol: newAsset.symbol,
            name: newAsset.name,
            category: newAsset.category,
            currentPrice: parseFloat(newAsset.currentPrice),
            quantity: parseFloat(newAsset.quantity),
            portfolioId: parseInt(id || ""),
        };

        try {
            const res = await fetch("/api/asset", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload),
            });

            if (res.status === 400) {
                const data = await res.json();
                const msg = Object.values(data.errors).flat().join(" ");
                setError(msg);
                return;
            }

            const added = await res.json();
            setAssets([...assets, added]);
            setNewAsset({ symbol: "", name: "", category: "", currentPrice: "", quantity: "" });
            setError("");
        } catch {
            setError("Błąd podczas dodawania aktywa.");
        }
    };

    const handleDelete = async () => {
        if (!deleteConfirm || password.length < 3) {
            setError("Musisz potwierdzić usunięcie i wpisać hasło.");
            return;
        }

        const confirmed = window.confirm("Czy na pewno chcesz usunąć ten portfel?");
        if (!confirmed) return;

        try {
            const res = await fetch(`/api/portfolio/${id}`, {
                method: "DELETE",
            });

            if (!res.ok) throw new Error();
            navigate("/dashboard");
        } catch {
            setError("Nie udało się usunąć portfela.");
        }
    };

    const handleEditPortfolio = async () => {
        if (!editName.trim()) {
            setError("Nazwa portfela jest wymagana.");
            return;
        }

        if (!portfolio) {
            setError("Nie można edytować nieistniejącego portfela.");
            return;
        }

        const updated: PortfolioDto = {
            id: portfolio.id,
            name: editName,
            description: editDescription,
            createdAt: portfolio.createdAt
        };

        try {
            const res = await fetch("/api/portfolio", {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(updated),
            });

            if (!res.ok) {
                const data = await res.json();
                const msg = data?.errors ? Object.values(data.errors).flat().join(" ") : "Błąd podczas zapisu.";
                setError(msg);
                return;
            }

            setPortfolio(updated);
            setEditing(false);
            setError("");
        } catch {
            setError("Wystąpił błąd przy aktualizacji.");
        }
    };

    if (!portfolio) return <p>Ładowanie...</p>;

    return (
        <div className="dashboard-content">
            <h2>{portfolio.name}</h2>
            <p>{portfolio.description}</p>

            <hr />
            <h3>Dodaj aktywo</h3>
            <div className="portfolio-form">
                <input
                    placeholder="Symbol"
                    value={newAsset.symbol}
                    onChange={(e) => setNewAsset({ ...newAsset, symbol: e.target.value })}
                />
                <input
                    placeholder="Nazwa"
                    value={newAsset.name}
                    onChange={(e) => setNewAsset({ ...newAsset, name: e.target.value })}
                />
                <input
                    placeholder="Kategoria (np. crypto, stock)"
                    value={newAsset.category}
                    onChange={(e) => setNewAsset({ ...newAsset, category: e.target.value })}
                />
                <input
                    placeholder="Aktualna cena"
                    type="number"
                    value={newAsset.currentPrice}
                    onChange={(e) => setNewAsset({ ...newAsset, currentPrice: e.target.value })}
                />
                <input
                    placeholder="Ilość"
                    type="number"
                    value={newAsset.quantity}
                    onChange={(e) => setNewAsset({ ...newAsset, quantity: e.target.value })}
                />
                <button className="save-btn" onClick={handleAddAsset}>
                    Dodaj aktywo
                </button>
            </div>

            <hr />
            <h3>Aktywa</h3>
            {assets.length === 0 ? (
                <p>Brak aktywów.</p>
            ) : (
                <div className="portfolios-list">
                    {assets.map((a) => (
                        <div key={a.id} className="portfolio-card">
                            <h4>{a.symbol} – {a.name}</h4>
                            <p>Kategoria: {a.category}</p>
                            <p>Cena: {a.currentPrice.toFixed(2)} | Ilość: {a.quantity}</p>
                        </div>
                    ))}
                </div>
            )}

            <hr />
            <h3>Edytuj portfel</h3>
            {!editing ? (
                <button className="save-btn" onClick={() => {
                    setEditName(portfolio.name);
                    setEditDescription(portfolio.description || "");
                    setEditing(true);
                }}>
                    Edytuj portfel
                </button>
            ) : (
                <div className="portfolio-form">
                    <input
                        value={editName}
                        onChange={(e) => setEditName(e.target.value)}
                        placeholder="Nazwa portfela"
                    />
                    <input
                        value={editDescription}
                        onChange={(e) => setEditDescription(e.target.value)}
                        placeholder="Opis portfela"
                    />
                    <button className="save-btn" onClick={handleEditPortfolio}>
                        Zapisz zmiany
                    </button>
                    <button onClick={() => setEditing(false)}>Anuluj</button>
                </div>
            )}

            <hr />
            <h3>Usuń portfel</h3>
            <div className="portfolio-form">
                <label>
                    <input
                        type="checkbox"
                        checked={deleteConfirm}
                        onChange={(e) => setDeleteConfirm(e.target.checked)}
                    />
                    Potwierdzam usunięcie
                </label>
                <input
                    type="password"
                    placeholder="Wpisz hasło"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                />
                <button className="save-btn" onClick={handleDelete}>
                    Usuń portfel
                </button>
            </div>

            {error && <div className="error-message">{error}</div>}
        </div>
    );
}