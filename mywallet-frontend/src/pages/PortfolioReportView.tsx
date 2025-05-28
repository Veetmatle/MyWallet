// src/pages/PortfolioReportView.tsx
import { useState, useEffect } from "react";
import { useParams, useNavigate, useSearchParams } from "react-router-dom";
import "./PortfolioReport.css";

interface TransactionDto {
    id: number;
    assetSymbol: string;
    price: number;
    quantity: number;
    totalAmount: number;
    type: string;
    executedAt: string;
    notes?: string;
}

export default function PortfolioReportView() {
    const { id } = useParams<{ id: string }>();
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    const [transactions, setTransactions] = useState<TransactionDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");

    const start = searchParams.get("start");
    const end = searchParams.get("end");

    useEffect(() => {
        async function fetchReport() {
            if (!id || !start || !end) return;

            setLoading(true);
            setError("");

            try {
                const res = await fetch(
                    `/api/transaction/portfolio/${id}/report?start=${encodeURIComponent(start)}&end=${encodeURIComponent(end)}`
                );
                if (!res.ok) throw new Error("Błąd pobierania raportu");
                const data: TransactionDto[] = await res.json();
                setTransactions(data);
            } catch (e: any) {
                setError(e.message || "Nie udało się pobrać raportu");
            } finally {
                setLoading(false);
            }
        }

        fetchReport();
    }, [id, start, end]);

    return (
        <div className="dashboard-content">
            <h2>Raport transakcji portfela</h2>

            {loading && <p>Ładowanie raportu...</p>}
            {error && <p style={{ color: "red" }}>{error}</p>}

            {transactions.length > 0 && (
                <>
                    <table
                        style={{
                            width: "100%",
                            marginTop: 20,
                            borderCollapse: "collapse",
                            border: "1px solid #ccc",
                        }}
                    >
                        <thead>
                        <tr>
                            <th style={{ border: "1px solid #ccc", padding: 6 }}>Data</th>
                            <th style={{ border: "1px solid #ccc", padding: 6 }}>Aktywo</th>
                            <th style={{ border: "1px solid #ccc", padding: 6 }}>Typ</th>
                            <th style={{ border: "1px solid #ccc", padding: 6 }}>Ilość</th>
                            <th style={{ border: "1px solid #ccc", padding: 6 }}>Cena</th>
                            <th style={{ border: "1px solid #ccc", padding: 6 }}>Kwota</th>
                            <th style={{ border: "1px solid #ccc", padding: 6 }}>Notatki</th>
                        </tr>
                        </thead>
                        <tbody>
                        {transactions.map((tx) => (
                            <tr key={tx.id}>
                                <td style={{ border: "1px solid #ccc", padding: 6 }}>
                                    {new Date(tx.executedAt).toLocaleDateString()}
                                </td>
                                <td style={{ border: "1px solid #ccc", padding: 6 }}>
                                    {tx.assetSymbol.toUpperCase()}
                                </td>
                                <td style={{ border: "1px solid #ccc", padding: 6 }}>{tx.type}</td>
                                <td style={{ border: "1px solid #ccc", padding: 6 }}>{tx.quantity}</td>
                                <td style={{ border: "1px solid #ccc", padding: 6 }}>
                                    ${tx.price.toFixed(2)}
                                </td>
                                <td style={{ border: "1px solid #ccc", padding: 6 }}>
                                    ${tx.totalAmount.toFixed(2)}
                                </td>
                                <td style={{ border: "1px solid #ccc", padding: 6 }}>
                                    {tx.notes || "-"}
                                </td>
                            </tr>
                        ))}
                        </tbody>
                    </table>

                    <div style={{ marginTop: 20 }}>
                        <button onClick={() => navigate(-1)}>Wróć</button>
                        <button style={{ marginLeft: 8 }} disabled>
                            Pobierz PDF (w trakcie implementacji)
                        </button>
                        <button style={{ marginLeft: 8 }} disabled>
                            Wyślij raport na mail (w trakcie implementacji)
                        </button>
                    </div>
                </>
            )}

            {transactions.length === 0 && !loading && !error && (
                <p>Brak transakcji w wybranym okresie.</p>
            )}
        </div>
    );
}
