import React, { useState, ChangeEvent, FormEvent, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
    getPortfolioById,
    PortfolioDto,
    uploadPortfolioImage,
} from "../api/portfolio";
import "./dashboard.css";

const PortfolioImageUpload: React.FC = () => {
    // Używamy generyka <{ id: string }> aby TS wiedział, że id jest stringiem lub undefined
    const { id } = useParams<{ id: string }>();
    const portfolioId = parseInt(id || "", 10);
    const navigate = useNavigate();

    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [portfolio, setPortfolio] = useState<PortfolioDto | null>(null);

    useEffect(() => {
        if (!isNaN(portfolioId)) {
            getPortfolioById(portfolioId)
                .then((data: PortfolioDto) => setPortfolio(data))
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
            // Po poprawnym uploadzie wracamy do dashboardu (gdzie widać miniaturkę)
            navigate("/dashboard");
        } catch (err) {
            console.error(err);
            setError("Wystąpił błąd podczas przesyłania pliku.");
        }
    };

    return (
        <div className="p-4 max-w-md mx-auto">
            <h2 className="text-xl font-semibold mb-4">
                {portfolio
                    ? `Dodaj/zmień zdjęcie dla: ${portfolio.name}`
                    : "Dodaj zdjęcie do portfela"}
            </h2>

            {error && <div className="text-red-500 mb-4">{error}</div>}

            <form onSubmit={handleSubmit} className="flex flex-col space-y-4">
                <input
                    type="file"
                    accept="image/*"
                    onChange={handleFileChange}
                    className="border p-2"
                />
                <button
                    type="submit"
                    className="bg-blue-600 text-white py-2 px-4 rounded-lg hover:bg-blue-700"
                >
                    Prześlij zdjęcie
                </button>
            </form>
        </div>
    );
};

export default PortfolioImageUpload;
