import axios from "axios";

axios.defaults.baseURL = "";
// Jeśli w package.json masz "proxy": "https://localhost:5001",
// baseURL może pozostać pusty i żądania do /api/ będziesz kierować na backend.

export interface PortfolioDto {
    id: number;
    name: string;
    description?: string;
    userId: number;
    createdAt: string;
    imagePath?: string | null;
}

// ----------------------------------------
// 1) Pobranie portfela po ID
// ----------------------------------------
export async function getPortfolioById(id: number): Promise<PortfolioDto> {
    const response = await axios.get<PortfolioDto>(`/api/portfolio/${id}`);
    return response.data;
}

// ----------------------------------------
// 2) Tworzenie nowego portfela
// ----------------------------------------
export async function createPortfolio(
    userId: number,
    name: string,
    description?: string
): Promise<PortfolioDto> {
    const response = await fetch("/api/portfolio", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({
            name,
            description,
            userId,
            createdAt: new Date().toISOString(),
        }),
    });

    if (!response.ok) {
        throw new Error("Nie udało się dodać portfela.");
    }

    return response.json();
}

// ----------------------------------------
// 3) Upload zdjęcia do portfela
// ----------------------------------------
export const uploadPortfolioImage = async (
    portfolioId: number,
    file: File
): Promise<PortfolioDto> => {
    const formData = new FormData();
    formData.append("file", file);

    const response = await axios.post<PortfolioDto>(
        `/api/portfolio/${portfolioId}/upload-image`,
        formData,
        {
            headers: {
                "Content-Type": "multipart/form-data",
            },
        }
    );
    return response.data;
};

// ----------------------------------------
// 4) Usuwanie portfela
// ----------------------------------------
export const deletePortfolio = async (id: number): Promise<void> => {
    await axios.delete(`/api/portfolio/${id}`);
};
