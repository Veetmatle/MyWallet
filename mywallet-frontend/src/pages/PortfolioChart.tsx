import { useParams, useNavigate } from "react-router-dom";
import { useEffect, useState } from "react";

export default function PortfolioChart() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const [imageSrc, setImageSrc] = useState<string | null>(null);
    const [error, setError] = useState<string>("");
    const [loading, setLoading] = useState<boolean>(true);
    const [startDate, setStartDate] = useState<string>("");
    const [endDate, setEndDate] = useState<string>("");

    useEffect(() => {
        // Ustaw domyślne daty przy załadowaniu
        const end = new Date();
        const start = new Date();
        start.setDate(start.getDate() - 30);

        setEndDate(end.toISOString().split("T")[0]);
        setStartDate(start.toISOString().split("T")[0]);
    }, []);

    const fetchChart = async () => {
        if (!id || !startDate || !endDate) return;

        try {
            setLoading(true);
            setError("");

            console.log('Requesting chart data:', { startDate, endDate });

            const response = await fetch(`/api/portfolio/${id}/chart?start=${startDate}&end=${endDate}`);

            // Sprawdź typ zawartości odpowiedzi
            const contentType = response.headers.get('content-type');

            if (!response.ok) {
                // Jeśli błąd, spróbuj odczytać JSON z komunikatem
                if (contentType?.includes('application/json')) {
                    const errorData = await response.json();
                    throw new Error(errorData.error || `Błąd HTTP: ${response.status}`);
                } else {
                    throw new Error(`Błąd HTTP: ${response.status}`);
                }
            }

            // Sprawdź czy rzeczywiście otrzymaliśmy obraz
            if (!contentType?.includes('image/png')) {
                throw new Error('Otrzymano nieprawidłowy format odpowiedzi (oczekiwano obrazu PNG)');
            }

            const blob = await response.blob();

            // Sprawdź czy blob nie jest pusty
            if (blob.size === 0) {
                throw new Error('Otrzymano pusty obraz');
            }

            // Zwolnij poprzedni URL jeśli istnieje
            if (imageSrc) {
                URL.revokeObjectURL(imageSrc);
            }

            const url = URL.createObjectURL(blob);
            setImageSrc(url);

        } catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Nieznany błąd';
            setError(`Nie udało się pobrać wykresu: ${errorMessage}`);
            console.error('Błąd podczas pobierania wykresu:', err);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        if (startDate && endDate) {
            fetchChart();
        }
    }, [id, startDate, endDate]);

    return (
        <div className="dashboard-content">
            <h2>Wykres portfela</h2>

            <div style={{ marginBottom: "20px" }}>
                <label>
                    Data początkowa:
                    <input
                        type="date"
                        value={startDate}
                        onChange={(e) => setStartDate(e.target.value)}
                        style={{ marginLeft: "10px", marginRight: "20px" }}
                    />
                </label>
                <label>
                    Data końcowa:
                    <input
                        type="date"
                        value={endDate}
                        onChange={(e) => setEndDate(e.target.value)}
                        style={{ marginLeft: "10px", marginRight: "20px" }}
                    />
                </label>
                <button
                    onClick={fetchChart}
                    style={{
                        marginLeft: "10px",
                        padding: "5px 15px",
                        backgroundColor: "#28a745",
                        color: "white",
                        border: "none",
                        borderRadius: "4px",
                        cursor: "pointer"
                    }}
                >
                    Odśwież wykres
                </button>
            </div>

            {error && (
                <div style={{
                    color: "red",
                    backgroundColor: "#ffe6e6",
                    padding: "10px",
                    borderRadius: "4px",
                    marginBottom: "20px"
                }}>
                    {error}
                </div>
            )}

            {loading ? (
                <p>Ładowanie wykresu...</p>
            ) : imageSrc ? (
                <div>
                    <img
                        src={imageSrc}
                        alt="Wykres portfela"
                        style={{
                            maxWidth: "100%",
                            height: "auto",
                            border: "1px solid #ddd",
                            borderRadius: "4px"
                        }}
                        onError={() => setError("Błąd podczas ładowania obrazu")}
                    />
                </div>
            ) : null}

            <button
                onClick={() => navigate(-1)}
                style={{
                    marginTop: 20,
                    padding: "10px 20px",
                    backgroundColor: "#007bff",
                    color: "white",
                    border: "none",
                    borderRadius: "4px",
                    cursor: "pointer"
                }}
            >
                Wróć
            </button>
        </div>
    );
}