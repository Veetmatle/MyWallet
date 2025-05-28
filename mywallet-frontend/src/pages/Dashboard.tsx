import { useEffect, useState } from "react";
import { createPortfolio } from "../api/portfolio";
import { Link } from "react-router-dom";
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
    const [formErrors, setFormErrors] = useState<{ [key: string]: string }>({});

    // --- Nowe stany dla usuwania ---
    const [showDeleteModal, setShowDeleteModal] = useState(false);
    const [selectedToDelete, setSelectedToDelete] = useState<number[]>([]);
    const [usernameConfirm, setUsernameConfirm] = useState("");
    const [deleteError, setDeleteError] = useState("");

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
        const errors: { [key: string]: string } = {};

        if (!newName.trim()) {
            errors.name = "Nazwa portfela jest wymagana.";
        } else if (newName.length > 100) {
            errors.name = "Nazwa może mieć maksymalnie 100 znaków.";
        }

        if (newDescription.length > 500) {
            errors.description = "Opis może mieć maksymalnie 500 znaków.";
        }

        setFormErrors(errors);
        if (Object.keys(errors).length > 0) return;

        try {
            const response = await fetch("/api/portfolio", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    name: newName,
                    description: newDescription,
                    userId: user.id,
                    createdAt: new Date().toISOString()
                }),
            });

            if (response.status === 400) {
                const data = await response.json();
                const backendErrors: { [key: string]: string } = {};
                for (const key in data.errors) {
                    backendErrors[key.toLowerCase()] = data.errors[key][0];
                }
                setFormErrors(backendErrors);
                return;
            }

            if (!response.ok) throw new Error("Nie udało się utworzyć portfela.");

            const created = await response.json();
            setPortfolios([...portfolios, created]);
            setNewName("");
            setNewDescription("");
            setFormErrors({});
            setShowForm(false);
        } catch (err: any) {
            setError(err.message);
        }
    };

    // --- Funkcje do usuwania ---

    // Zaznaczanie/odznaczanie portfeli do usunięcia
    const toggleSelectToDelete = (id: number) => {
        setSelectedToDelete(prev =>
            prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]
        );
    };

    // Potwierdzenie i usunięcie portfeli
    const handleDeletePortfolios = async () => {
        setDeleteError("");

        if (selectedToDelete.length === 0) {
            setDeleteError("Wybierz przynajmniej jeden portfel.");
            return;
        }

        if (usernameConfirm.trim().toLowerCase() !== user.username.toLowerCase()) {
            setDeleteError("Niepoprawna nazwa użytkownika.");
            return;
        }

        try {
            for (const id of selectedToDelete) {
                const res = await fetch(`/api/portfolio/${id}`, { method: "DELETE" });
                if (!res.ok) {
                    const text = await res.text();
                    throw new Error(text || "Błąd usuwania portfela.");
                }
            }

            // Po usunięciu odśwież listę portfeli
            const remaining = portfolios.filter(p => !selectedToDelete.includes(p.id));
            setPortfolios(remaining);

            // Zamknij modal i wyczyść stany
            setShowDeleteModal(false);
            setSelectedToDelete([]);
            setUsernameConfirm("");
            setDeleteError("");
        } catch (err: any) {
            setDeleteError(err.message);
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
                <div className="action-buttons">
                    <button className="add-btn" onClick={() => setShowForm(!showForm)}>
                        {showForm ? "Anuluj" : "+ Dodaj nowy portfel"}
                    </button>

                    <button
                        className="delete-portfolio-btn"
                        onClick={() => setShowDeleteModal(true)}
                    >
                        Usuń portfel
                    </button>
                </div>

                {showForm && (
                    <div className="portfolio-form">
                        <input
                            type="text"
                            placeholder="Nazwa portfela"
                            value={newName}
                            onChange={(e) => setNewName(e.target.value)}
                        />
                        {formErrors.name && <div className="error-message">{formErrors.name}</div>}

                        <input
                            type="text"
                            placeholder="Opis (opcjonalnie)"
                            value={newDescription}
                            onChange={(e) => setNewDescription(e.target.value)}
                        />
                        {formErrors.description && (
                            <div className="error-message">{formErrors.description}</div>
                        )}
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
                                Stwórz swój pierwszy portfel, aby zacząć śledzić swoje inwestycje i
                                oszczędności.
                            </p>
                        </div>
                    </div>
                ) : (
                    <div className="portfolios-list">
                        {portfolios.map((portfolio) => (
                            <Link
                                to={`/portfolio/${portfolio.id}`}
                                key={portfolio.id}
                                className="portfolio-card-link"
                            >
                                <div className="portfolio-card">
                                    <h3 className="portfolio-name">{portfolio.name}</h3>
                                    <p className="portfolio-description">
                                        {portfolio.description || "Brak opisu"}
                                    </p>
                                    <small className="portfolio-date">
                                        Utworzono: {formatDate(portfolio.createdAt)}
                                    </small>
                                </div>
                            </Link>
                        ))}
                    </div>
                )}

                {/* MODAL USUWANIA PORTFELI */}
                {showDeleteModal && (
                    <div className="modal-overlay">
                        <div className="modal">
                            <h3>Usuń portfel(e)</h3>
                            <div className="portfolio-list">
                                {portfolios.map((p) => (
                                    <label key={p.id}>
                                        <input
                                            type="checkbox"
                                            checked={selectedToDelete.includes(p.id)}
                                            onChange={() => toggleSelectToDelete(p.id)}
                                        />
                                        {p.name}
                                    </label>
                                ))}
                            </div>
                            <input
                                type="text"
                                placeholder="Wpisz swoją nazwę użytkownika, aby potwierdzić"
                                value={usernameConfirm}
                                onChange={(e) => setUsernameConfirm(e.target.value)}
                            />
                            {deleteError && <div className="error-message">{deleteError}</div>}
                            <div className="modal-buttons">
                                <button className="delete-btn" onClick={handleDeletePortfolios}>Usuń</button>
                                <button className="cancel-btn" onClick={() => setShowDeleteModal(false)}>Anuluj</button>
                            </div>
                        </div>
                    </div>
                )}

            </div>
        </div>
    );
}
