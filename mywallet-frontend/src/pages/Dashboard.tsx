import { useEffect, useState } from "react";
import { createPortfolio } from "../api/portfolio"; // zakładam, że masz ten plik
import "./dashboard.css";

interface PortfolioDto {
    id: number;
    name: string;
    description?: string;
    createdAt: string;
}

export default function Dashboard() {
    const [portfolios, setPortfolios] = useState<PortfolioDto[]>([]);
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(true);
    const [user, setUser] = useState<any>(null);

    const [showForm, setShowForm] = useState(false);
    const [newName, setNewName] = useState("");
    const [newDescription, setNewDescription] = useState("");

    useEffect(() => {
        const userData = localStorage.getItem("user");
        if (!userData) {
            window.location.href = "/";
            return;
        }

        const parsedUser = JSON.parse(userData);
        setUser(parsedUser);

        setLoading(true);
        fetch(`/api/portfolio/user/${parsedUser.id}`)
            .then((res) => {
                if (!res.ok) throw new Error("Nie udało się pobrać portfeli");
                return res.json();
            })
            .then((data) => {
                setPortfolios(data);
                setLoading(false);
            })
            .catch((err) => {
                setError(err.message);
                setLoading(false);
            });
    }, []);

    const handleLogout = () => {
        localStorage.removeItem("user");
        window.location.href = "/";
    };

    const handleCreatePortfolio = async () => {
        if (!newName.trim()) return;

        try {
            const created = await createPortfolio(user.id, newName, newDescription);
            setPortfolios([...portfolios, created]);
            setNewName("");
            setNewDescription("");
            setShowForm(false);
        } catch (err: any) {
            setError(err.message);
        }
    };

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return new Intl.DateTimeFormat("pl-PL", {
            day: "numeric",
            month: "long",
            year: "numeric",
        }).format(date);
    };

    if (loading) {
        return (
            <div className="dashboard-container">
                <div className="dashboard-header">
                    <div className="dashboard-logo">
                        <img src="/logo192.png" alt="MyWallet Logo" />
                        <span>MyWallet</span>
                    </div>
                </div>
                <div className="dashboard-content">
                    <p>Ładowanie danych...</p>
                </div>
            </div>
        );
    }

    return (
        <div className="dashboard-container">
            <div className="dashboard-header">
                <div className="dashboard-logo">
                    <img src="/logo192.png" alt="MyWallet Logo" />
                    <span>MyWallet</span>
                </div>
                <div className="dashboard-nav">
                    {user && (
                        <div className="user-info">
                            <span className="user-name">
                                Witaj, {user.username || "Użytkowniku"}
                            </span>
                            <button onClick={handleLogout} className="logout-btn">
                                Wyloguj
                            </button>
                        </div>
                    )}
                </div>
            </div>

            <div className="dashboard-content">
                <h2 className="page-title">Twoje Portfele</h2>
                <button className="add-btn" onClick={() => setShowForm(!showForm)}>
                    {showForm ? "Anuluj" : "+ Dodaj nowy portfel"}
                </button>

                {showForm && (
                    <div className="portfolio-form">
                        <input
                            type="text"
                            placeholder="Nazwa portfela"
                            value={newName}
                            onChange={(e) => setNewName(e.target.value)}
                        />
                        <input
                            type="text"
                            placeholder="Opis (opcjonalnie)"
                            value={newDescription}
                            onChange={(e) => setNewDescription(e.target.value)}
                        />
                        <button className="save-btn" onClick={handleCreatePortfolio}>
                            Zapisz portfel
                        </button>
                    </div>
                )}

                {error && <div className="error-message">{error}</div>}

                {portfolios.length === 0 ? (
                    <div className="empty-state">
                        <div className="empty-state-icon">📊</div>
                        <div className="empty-state-text">
                            <h3>Nie masz jeszcze żadnych portfeli</h3>
                            <p>
                                Stwórz swój pierwszy portfel, aby zacząć śledzić swoje
                                inwestycje i oszczędności.
                            </p>
                        </div>
                    </div>
                ) : (
                    <div className="portfolios-list">
                        {portfolios.map((portfolio) => (
                            <div key={portfolio.id} className="portfolio-card">
                                <h3 className="portfolio-name">{portfolio.name}</h3>
                                <p className="portfolio-description">
                                    {portfolio.description || "Brak opisu"}
                                </p>
                                <small className="portfolio-date">
                                    Utworzono: {formatDate(portfolio.createdAt)}
                                </small>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}
