﻿import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import "./dashboard.css";

interface PortfolioDto {
    id: number;
    name: string;
    description?: string;
    createdAt: string;
    imagePath?: string | null;
}

interface User {
    userId: number;
    username: string;
    email: string;
    firstName?: string;
    lastName?: string;
    isAdmin: boolean;
}

interface AdminUser {
    userId: number;
    username: string;
    email: string;
    firstName?: string;
    lastName?: string;
    isAdmin: boolean;
}

export default function Dashboard() {
    const [portfolios, setPortfolios] = useState<PortfolioDto[]>([]);
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(true);
    const [user, setUser] = useState<any>(null);
    const [isAdmin, setIsAdmin] = useState(false);
    const [adminUsers, setAdminUsers] = useState<AdminUser[]>([]);
    const [adminLoading, setAdminLoading] = useState(false);

    const [showForm, setShowForm] = useState(false);
    const [newName, setNewName] = useState("");
    const [newDescription, setNewDescription] = useState("");
    const [formErrors, setFormErrors] = useState<{ [key: string]: string }>({});

    // Stany dla usuwania
    const [showDeleteModal, setShowDeleteModal] = useState(false);
    const [selectedToDelete, setSelectedToDelete] = useState<number[]>([]);
    const [usernameConfirm, setUsernameConfirm] = useState("");
    const [deleteError, setDeleteError] = useState("");

    const navigate = useNavigate();

    useEffect(() => {
        const userData = localStorage.getItem("user");
        if (!userData) {
            window.location.href = "/";
            return;
        }

        const parsedUser = JSON.parse(userData);
        setUser(parsedUser);

        // Sprawdź status administratora
        checkAdminStatus();

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

    const checkAdminStatus = async () => {
        try {
            const response = await fetch('/api/user/current');
            if (response.ok) {
                const userData = await response.json();
                setIsAdmin(userData.isAdmin);
                if (userData.isAdmin) {
                    fetchAdminUsers();
                }
            }
        } catch (error) {
            console.error('Błąd sprawdzania statusu admina:', error);
        }
    };

    const fetchAdminUsers = async () => {
        setAdminLoading(true);
        try {
            const response = await fetch('/api/admin/users');
            if (response.ok) {
                const users = await response.json();
                setAdminUsers(users);
            }
        } catch (error) {
            console.error('Błąd pobierania użytkowników:', error);
        }
        setAdminLoading(false);
    };

    const makeAdmin = async (userId: number) => {
        try {
            const response = await fetch(`/api/admin/make-admin/${userId}`, {
                method: 'POST',
            });
            if (response.ok) {
                fetchAdminUsers();
            }
        } catch (error) {
            console.error('Błąd nadawania uprawnień admina:', error);
        }
    };

    const removeAdmin = async (userId: number) => {
        try {
            const response = await fetch(`/api/admin/remove-admin/${userId}`, {
                method: 'POST',
            });
            if (response.ok) {
                fetchAdminUsers();
            }
        } catch (error) {
            console.error('Błąd usuwania uprawnień admina:', error);
        }
    };

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
                    createdAt: new Date().toISOString(),
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

            const created: PortfolioDto = await response.json();
            setPortfolios([...portfolios, created]);
            setNewName("");
            setNewDescription("");
            setFormErrors({});
            setShowForm(false);
        } catch (err: any) {
            setError(err.message);
        }
    };

    // Toggle zaznaczenia portfeli do usunięcia
    const toggleSelectToDelete = (id: number) => {
        setSelectedToDelete((prev) =>
            prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
        );
    };

    // Usunięcie zaznaczonych portfeli
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

            const remaining = portfolios.filter(
                (p) => !selectedToDelete.includes(p.id)
            );
            setPortfolios(remaining);

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

    // Panel administratora
    if (isAdmin) {
        return (
            <div className="dashboard-container">
                <div className="dashboard-header">
                    <div className="dashboard-logo">
                        <img src="/logo192.png" alt="MyWallet Logo" />
                        <span>MyWallet - Panel Administratora</span>
                    </div>
                    <div className="dashboard-nav">
                        {user && (
                            <div className="user-info">
                                <span className="user-name">
                                    Admin: {user.username || "Użytkownik"}
                                </span>
                                <button onClick={handleLogout} className="logout-btn">
                                    Wyloguj
                                </button>
                            </div>
                        )}
                    </div>
                </div>

                <div className="dashboard-content">
                    <h2 className="page-title">Zarządzanie użytkownikami</h2>

                    {adminLoading ? (
                        <p>Ładowanie użytkowników...</p>
                    ) : (
                        <div className="admin-panel">
                            <table className="admin-table">
                                <thead>
                                <tr>
                                    <th>ID</th>
                                    <th>Nazwa użytkownika</th>
                                    <th>Email</th>
                                    <th>Imię</th>
                                    <th>Nazwisko</th>
                                    <th>Status</th>
                                    <th>Akcje</th>
                                </tr>
                                </thead>
                                <tbody>
                                {adminUsers.map(adminUser => (
                                    <tr key={adminUser.userId}>
                                        <td>{adminUser.userId}</td>
                                        <td>{adminUser.username}</td>
                                        <td>{adminUser.email}</td>
                                        <td>{adminUser.firstName || '-'}</td>
                                        <td>{adminUser.lastName || '-'}</td>
                                        <td>
                                            {adminUser.isAdmin ? (
                                                <span className="admin-badge">Administrator</span>
                                            ) : (
                                                <span className="user-badge">Użytkownik</span>
                                            )}
                                        </td>
                                        <td>
                                            {!adminUser.isAdmin ? (
                                                <button
                                                    className="make-admin-btn"
                                                    onClick={() => makeAdmin(adminUser.userId)}
                                                >
                                                    Zrób admina
                                                </button>
                                            ) : (
                                                <button
                                                    className="remove-admin-btn"
                                                    onClick={() => removeAdmin(adminUser.userId)}
                                                >
                                                    Usuń admina
                                                </button>
                                            )}
                                        </td>
                                    </tr>
                                ))}
                                </tbody>
                            </table>
                        </div>
                    )}
                </div>
            </div>
        );
    }

    // Normalny dashboard dla zwykłych użytkowników
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
                        {formErrors.name && (
                            <div className="error-message">{formErrors.name}</div>
                        )}

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
                                Stwórz swój pierwszy portfel, aby zacząć śledzić swoje
                                inwestycje i oszczędności.
                            </p>
                        </div>
                    </div>
                ) : (
                    <div className="portfolios-list">
                        {portfolios.map((portfolio) => (
                            <div key={portfolio.id} className="portfolio-card-container">
                                <div className="portfolio-card">
                                    {/* Sekcja INFORMACYJNA */}
                                    <Link
                                        to={`/portfolio/${portfolio.id}`}
                                        className="portfolio-info-link"
                                    >
                                        <div className="portfolio-info">
                                            <p className="portfolio-date">
                                                Utworzono: {formatDate(portfolio.createdAt)}
                                            </p>
                                            <h3 className="portfolio-name">{portfolio.name}</h3>
                                            <p className="portfolio-description">
                                                {portfolio.description || "Brak opisu"}
                                            </p>
                                        </div>
                                    </Link>

                                    {/* Sekcja OBRAZKA (kliknięcie przenosi do uploadu) */}
                                    <div
                                        className="portfolio-image-wrapper"
                                        onClick={() =>
                                            navigate(`/portfolio/${portfolio.id}/upload-image`)
                                        }
                                    >
                                        {portfolio.imagePath ? (
                                            <img
                                                src={portfolio.imagePath}
                                                alt="Zdjęcie portfela"
                                                className="portfolio-thumbnail"
                                            />
                                        ) : (
                                            <div className="no-image-placeholder">
                                                Brak zdjęcia
                                            </div>
                                        )}
                                    </div>
                                </div>
                            </div>
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
                            {deleteError && (
                                <div className="error-message">{deleteError}</div>
                            )}
                            <div className="modal-buttons">
                                <button
                                    className="delete-btn"
                                    onClick={handleDeletePortfolios}
                                >
                                    Usuń
                                </button>
                                <button
                                    className="cancel-btn"
                                    onClick={() => setShowDeleteModal(false)}
                                >
                                    Anuluj
                                </button>
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}
