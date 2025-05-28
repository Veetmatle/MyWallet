// src/pages/PortfolioReport.tsx
import { useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
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

const DATE_OPTIONS = [
    { label: "Ostatni tydzień", value: "week" },
    { label: "Ostatni miesiąc", value: "month" },
    { label: "Ostatni rok", value: "year" },
];

function getDateRange(option: string) {
    const end = new Date();
    const start = new Date();

    switch (option) {
        case "week":
            start.setDate(end.getDate() - 7);
            break;
        case "month":
            start.setMonth(end.getMonth() - 1);
            break;
        case "year":
            start.setFullYear(end.getFullYear() - 1);
            break;
        default:
            start.setDate(end.getDate() - 7);
    }

    return {
        start: start.toISOString().slice(0, 10),
        end: end.toISOString().slice(0, 10),
    };
}

export default function PortfolioReport() {
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();

    const [dateOption, setDateOption] = useState("week");
    const [transactions, setTransactions] = useState<TransactionDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");

    const fetchReport = async () => {
        if (!id) return;
        setLoading(true);
        setError("");

        const { start, end } = getDateRange(dateOption);

        try {
            const res = await fetch(
                `/api/transaction/portfolio/${id}/report?start=${start}&end=${end}`
            );
            if (!res.ok) throw new Error("Błąd pobierania raportu");
            const data: TransactionDto[] = await res.json();
            setTransactions(data);
        } catch (e: any) {
            setError(e.message || "Nie udało się pobrać raportu");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="dashboard-content">
            <h2>Raport transakcji portfela</h2>

            <label>Wybierz zakres:</label>
            <select
                value={dateOption}
                onChange={(e) => setDateOption(e.target.value)}
                style={{ marginLeft: 10 }}
            >
                {DATE_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                        {opt.label}
                    </option>
                ))}
            </select>

            <div style={{ marginTop: 16 }}>
                <button onClick={fetchReport}>OK</button>
                <button onClick={() => navigate(-1)} style={{ marginLeft: 8 }}>
                    Anuluj / Wróć
                </button>
            </div>

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
        </div>
    );
}
