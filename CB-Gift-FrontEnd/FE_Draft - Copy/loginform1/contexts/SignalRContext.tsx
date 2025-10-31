// contexts/SignalRContext.tsx

"use client"; // Rất quan trọng: Bắt buộc phải là Client Component

import React, { createContext, useContext, useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';

// 1. Tạo Context để chứa đối tượng connection
const SignalRContext = createContext<HubConnection | null>(null);

// 2. Tạo Provider để khởi tạo và cung cấp connection cho toàn bộ ứng dụng
export const SignalRProvider = ({ children }: { children: React.ReactNode }) => {
    const [connection, setConnection] = useState<HubConnection | null>(null);

    useEffect(() => {
        // Lấy token xác thực của người dùng (giả sử lưu trong localStorage)
        const token = localStorage.getItem('authToken'); 

        // Chỉ tạo kết nối khi người dùng đã đăng nhập (có token)
        if (token) {
            const newConnection = new signalR.HubConnectionBuilder()
                .withUrl("https://localhost:7015/notificationHub", {
                    // Gửi token lên server để xác thực
                    accessTokenFactory: () => token 
                })
                .withAutomaticReconnect() // Tự động kết nối lại
                .build();
            
            setConnection(newConnection);
        }
    }, []); // Mảng rỗng đảm bảo useEffect này chỉ chạy 1 lần duy nhất

    // Effect này để khởi động kết nối
    useEffect(() => {
        if (connection && connection.state === HubConnectionState.Disconnected) {
            connection.start()
                .then(() => console.log('✅ SignalR Connected successfully!'))
                .catch(e => console.error('SignalR Connection failed: ', e));
        }

        // Dọn dẹp: Ngắt kết nối khi component bị unmount
        return () => {
            connection?.stop();
        };
    }, [connection]);

    return (
        <SignalRContext.Provider value={connection}>
            {children}
        </SignalRContext.Provider>
    );
};

// 3. Tạo một custom hook để các component con dễ dàng sử dụng
export const useSignalR = () => {
    return useContext(SignalRContext);
};