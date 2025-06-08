// src/pages/PortfolioImageUpload.tsx
import React, { useState, ChangeEvent, FormEvent, useEffect } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import {
    getPortfolioById,
    PortfolioDto,
    uploadPortfolioImage,
} from "../api/portfolio";
import "./dashboard.css";

const PortfolioImageUpload: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const portfolioId = parseInt(id || "", 10);
    const navigate = useNavigate();

    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [portfolio, setPortfolio] = useState<PortfolioDto | null>(null);

    useEffect(() => {
        if (!isNaN(portfolioId)) {
            getPortfolioById(portfolioId)
                .then((data) => setPortfolio(data))
                .catch(() => setError("Nie udało się pobrać danych portfela."));
        } else {
            setError("Nieprawidłowy identyfikator portfela.");
        }
    }, [portfolioId]);

    const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
        if (e.target.files && e.target.files.length > 0) {
            setSelectedFile(e.target.files[0]);
        }
    };

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setError(null);

        if (!selectedFile) {
            setError("Proszę wybrać plik.");
            return;
        }

        try {
            await uploadPortfolioImage(portfolioId, selectedFile);
            navigate("/dashboard");
        } catch {
            setError("Wystąpił błąd podczas przesyłania pliku.");
        }
    };

    return (
        <div className="dashboard-container">
            {/* Header z linkiem powrotu */}
            <div className="dashboard-header">
                <Link to="/dashboard" className="dashboard-logo" style={{ textDecoration: 'none' }}>
                    ← Powrót do kokpitu
                </Link>
            </div>

            <div className="dashboard-content">
                <div className="portfolio-image-upload" style={{ maxWidth: 600, margin: "0 auto" }}>
                    <h2 style={{ marginBottom: 20 }}>
                        {portfolio
                            ? `Dodaj/zmień zdjęcie dla: ${portfolio.name}`
                            : "Dodaj zdjęcie do portfela"}
                    </h2>

                    {error && (
                        <div className="error-message" style={{ marginBottom: 16 }}>
                            {error}
                        </div>
                    )}

                    <form
                        onSubmit={handleSubmit}
                        style={{ display: "flex", gap: 12, alignItems: "center" }}
                    >
                        <input
                            type="file"
                            accept="image/*"
                            onChange={handleFileChange}
                            style={{
                                flex: 1,
                                padding: "8px 12px",
                                border: "1px solid #ccc",
                                borderRadius: 4,
                            }}
                        />
                        <button
                            type="submit"
                            className="add-btn"
                            style={{ padding: "10px 20px" }}
                        >
                            Prześlij zdjęcie
                        </button>
                    </form>
                </div>
            </div>
        </div>
    );
};

export default PortfolioImageUpload;
