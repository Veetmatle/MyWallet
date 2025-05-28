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
    const [sendingMail, setSendingMail] = useState(false);   // nowy stan dla wysyłania maila
    const [mailError, setMailError] = useState("");           // błąd wysyłania maila
    const [mailSuccess, setMailSuccess] = useState("");       // komunikat o sukcesie

    const start = searchParams.get("start");
    const end = searchParams.get("end");

    useEffect(() => {
        async function fetchReport() {
            if (!id || !start || !end) return;

            setLoading(true);
            setError("");
            setMailError("");
            setMailSuccess("");

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

    // Funkcja do pobierania PDF (już masz)
    const downloadPdf = async () => {
        if (!id || !start || !end) return;
        try {
            const res = await fetch(
                `/api/transaction/portfolio/${id}/report/pdf?start=${encodeURIComponent(start)}&end=${encodeURIComponent(end)}`,
                { method: "GET" }
            );
            if (!res.ok) throw new Error("Błąd pobierania PDF");
            const blob = await res.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = url;
            a.download = `Report_Portfolio_${id}_${start}_${end}.pdf`;
            document.body.appendChild(a);
            a.click();
            a.remove();
            window.URL.revokeObjectURL(url);
        } catch (e: any) {
            alert(e.message || "Błąd podczas pobierania PDF");
        }
    };

    // Funkcja do wysyłania maila z raportem
    const sendReportEmail = async () => {
        if (!id || !start || !end) return;
        setSendingMail(true);
        setMailError("");
        setMailSuccess("");
        try {
            const res = await fetch(
                `/api/transaction/portfolio/${id}/report/sendmail?start=${encodeURIComponent(start)}&end=${encodeURIComponent(end)}`,
                { method: "POST" }
            );
            if (!res.ok) {
                const text = await res.text();
                throw new Error(text || "Błąd podczas wysyłania maila");
            }
            setMailSuccess("Raport został wysłany na Twój adres e-mail.");
        } catch (e: any) {
            setMailError(e.message || "Błąd podczas wysyłania maila");
        } finally {
            setSendingMail(false);
        }
    };

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
                        <button style={{ marginLeft: 8 }} onClick={downloadPdf}>
                            Pobierz PDF
                        </button>
                        <button style={{ marginLeft: 8 }} onClick={sendReportEmail} disabled={sendingMail}>
                            {sendingMail ? "Wysyłanie..." : "Wyślij raport na mail"}
                        </button>
                    </div>
                    {mailError && <p style={{ color: "red" }}>{mailError}</p>}
                    {mailSuccess && <p style={{ color: "green" }}>{mailSuccess}</p>}
                </>
            )}

            {transactions.length === 0 && !loading && !error && (
                <p>Brak transakcji w wybranym okresie.</p>
            )}
        </div>
    );
}
