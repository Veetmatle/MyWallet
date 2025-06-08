import React, { useState, useEffect } from 'react';

interface User {
    userId: number;
    username: string;
    email: string;
    isAdmin: boolean;
}

const AdminPanel: React.FC = () => {
    const [users, setUsers] = useState<User[]>([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetchUsers();
    }, []);

    const fetchUsers = async () => {
        try {
            const response = await fetch('/api/admin/users', {
                credentials: 'include'
            });
            const data = await response.json();
            setUsers(data);
            setLoading(false);
        } catch (error) {
            console.error('Błąd pobierania użytkowników:', error);
            setLoading(false);
        }
    };

    const makeAdmin = async (userId: number) => {
        try {
            await fetch(`/api/admin/make-admin/${userId}`, {
                method: 'POST',
                credentials: 'include'
            });
            fetchUsers();
        } catch (error) {
            console.error('Błąd nadawania uprawnień admina:', error);
        }
    };

    const removeAdmin = async (userId: number) => {
        try {
            await fetch(`/api/admin/remove-admin/${userId}`, {
                method: 'POST',
                credentials: 'include'
            });
            fetchUsers();
        } catch (error) {
            console.error('Błąd usuwania uprawnień admina:', error);
        }
    };

    if (loading) {
        return <div>Ładowanie...</div>;
    }

    return (
        <div className="admin-panel">
            <h2>Zarządzanie użytkownikami</h2>

            <table className="table">
                <thead>
                <tr>
                    <th>ID</th>
                    <th>Nazwa użytkownika</th>
                    <th>Email</th>
                    <th>Status</th>
                    <th>Akcje</th>
                </tr>
                </thead>
                <tbody>
                {users.map(user => (
                    <tr key={user.userId}>
                        <td>{user.userId}</td>
                        <td>{user.username}</td>
                        <td>{user.email}</td>
                        <td>
                            {user.isAdmin ? 'Administrator' : 'Użytkownik'}
                        </td>
                        <td>
                            {!user.isAdmin ? (
                                <button
                                    onClick={() => makeAdmin(user.userId)}
                                    className="btn btn-primary"
                                >
                                    Zrób admina
                                </button>
                            ) : (
                                <button
                                    onClick={() => removeAdmin(user.userId)}
                                    className="btn btn-warning"
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
    );
};

export default AdminPanel;
