"use client";

import React, { useState, useEffect } from 'react';

// --- Icon Components (self-contained SVGs for easy use) ---
const FilterIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="mr-2 text-gray-500">
        <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"></polygon>
    </svg>
);
const DownloadIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" /><polyline points="7 10 12 15 17 10" /><line x1="12" y1="15" x2="12" y2="3" /></svg>
);
const CardIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M22 18a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v12Z" /><path d="M6 12h12" /></svg>
);
const LabelIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z" /><line x1="7" y1="7" x2="7.01" y2="7" /></svg>
);
const NeedsProductionIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M20 17.58A5 5 0 0 0 18 8h-1.26A8 8 0 1 0 4 16.25" /><line x1="8" y1="16" x2="8" y2="22" /><line x1="16" y1="16" x2="16" y2="22" /><line x1="12" y1="12" x2="12" y2="22" /></svg>
);
const ProducedIcon = () => (
    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polyline points="9 11 12 14 22 4"></polyline><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"></path></svg>
);


// --- Sidebar Component ---
const ProductionSidebar = ({ statusFilter, setStatusFilter }) => {
    const navItems = [
        { id: 'needs_production', label: 'Needs Production', icon: <NeedsProductionIcon /> },
        { id: 'produced', label: 'Produced', icon: <ProducedIcon /> },
    ];

    return (
        <aside className="w-64 bg-white border-r border-gray-200 flex-shrink-0">
            <div className="p-4">
                <h2 className="text-lg font-bold text-gray-800">Production Status</h2>
            </div>
            <nav className="mt-2">
                <ul>
                    {navItems.map(item => (
                        <li key={item.id} className="px-4">
                            <button
                                onClick={() => setStatusFilter(item.id)}
                                className={`w-full flex items-center p-3 my-1 rounded-lg text-left text-sm font-medium transition-colors duration-200 ${
                                    statusFilter === item.id 
                                    ? 'bg-blue-500 text-white shadow' 
                                    : 'text-gray-600 hover:bg-gray-100'
                                }`}
                            >
                                <span className="mr-3">{item.icon}</span>
                                {item.label}
                            </button>
                        </li>
                    ))}
                </ul>
            </nav>
        </aside>
    );
};


// --- Main Page Component ---
export default function StaffProductionStatusPage() {
    const [productionData, setProductionData] = useState([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState(null);
    const [selectedCategory, setSelectedCategory] = useState(null);
    const [selectedDate, setSelectedDate] = useState('');
    const [statusFilter, setStatusFilter] = useState('needs_production');

    // --- Data Fetching Effect ---
    useEffect(() => {
        const fetchProductionData = async () => {
            setIsLoading(true);
            setError(null);
            
            const params = new URLSearchParams();
            if (selectedCategory) params.append('categoryId', selectedCategory);
            if (selectedDate) params.append('selectedDate', selectedDate);
            if (statusFilter) params.append('status', statusFilter);

            try {
                const response = await fetch(`https://localhost:7015/api/plan/staff-view?${params.toString()}`);
                if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
                const data = await response.json();
                console.log("Fetched production data:", data);
                setProductionData(data);
            } catch (e) {
                console.error("Failed to fetch production data:", e);
                setError("Could not load production data. Please try again later.");
            } finally {
                setIsLoading(false);
            }
        };

        fetchProductionData();
    }, [selectedCategory, selectedDate, statusFilter]);

    const getUniqueCategories = () => {
        const categories = productionData.map(item => ({ id: item.categoryId, name: item.categoryName }));
        return [...new Map(categories.map(item => [item.id, item])).values()];
    };

    const renderSkeletonLoader = () => (
        <div className="space-y-6 animate-pulse">
            <div className="h-10 bg-gray-200 rounded-md w-1/3"></div>
            <div className="bg-white p-4 rounded-lg shadow-md">
                <div className="h-8 bg-yellow-200 rounded-md w-1/4 mb-4"></div>
                <div className="space-y-3">
                    <div className="grid grid-cols-6 gap-4">
                        <div className="h-4 bg-gray-200 rounded col-span-1"></div>
                        <div className="h-4 bg-gray-200 rounded col-span-2"></div>
                        <div className="h-4 bg-gray-200 rounded col-span-1"></div>
                        <div className="h-4 bg-gray-200 rounded col-span-2"></div>
                    </div>
                </div>
            </div>
        </div>
    );

    return (
        <div className="flex h-screen overflow-hidden">
            <ProductionSidebar statusFilter={statusFilter} setStatusFilter={setStatusFilter} />
            
            <main className="flex-1 overflow-y-auto bg-gray-50 p-6">
                <div className="max-w-7xl mx-auto">
                    {/* Page Header */}
                    <div className="mb-8">
                        <h1 className="text-3xl font-bold text-gray-800">Update Production Status</h1>
                        <p className="text-gray-500 mt-1">Track and manage orders in the production pipeline.</p>
                    </div>

                    {/* Filters */}
                    <div className="bg-white p-4 rounded-lg shadow-sm border border-gray-200 mb-8 flex items-center flex-wrap gap-4">
                        <FilterIcon />
                        <h3 className="text-md font-semibold text-gray-700 mr-4">Filters:</h3>
                        
                        <select
                            value={selectedCategory || ''}
                            onChange={(e) => setSelectedCategory(e.target.value ? parseInt(e.target.value) : null)}
                            className="bg-gray-100 border-gray-300 rounded-md shadow-sm p-2 text-sm focus:ring-blue-500 focus:border-blue-500"
                        >
                            <option value="">All Product Types</option>
                            {getUniqueCategories().map(cat => (
                               <option key={cat.id} value={cat.id}>{cat.name}</option> 
                            ))}
                        </select>

                        <input
                            type="date"
                            value={selectedDate}
                            onChange={(e) => setSelectedDate(e.target.value)}
                            className="bg-gray-100 border-gray-300 rounded-md shadow-sm p-2 text-sm focus:ring-blue-500 focus:border-blue-500"
                        />
                        <button 
                            onClick={() => { setSelectedCategory(null); setSelectedDate(''); }}
                            className="text-sm text-blue-600 hover:underline ml-auto pr-2"
                        >
                            Clear Filters
                        </button>
                    </div>

                    {/* Content Area */}
                    <div className="space-y-8">
                        {isLoading ? (
                            renderSkeletonLoader()
                        ) : error ? (
                            <div className="text-center py-10 bg-white rounded-lg shadow-md">
                                <p className="text-red-500 font-semibold">{error}</p>
                            </div>
                        ) : productionData.length === 0 ? (
                             <div className="text-center py-10 bg-white rounded-lg shadow-md">
                                 <p className="text-gray-500">No production orders found with the current filters.</p>
                             </div>
                        ) : (
                            productionData.map(category => (
                                <div key={category.categoryId}>
                                    <h2 className="text-2xl font-bold text-gray-700">
                                        {category.categoryName} 
                                        <span className="text-lg font-medium text-gray-500 ml-2">({category.totalItems} orders)</span>
                                    </h2>
                                    <div className="mt-4 space-y-6">
                                        {category.dateGroups.map(dateGroup => (
                                            <div key={dateGroup.groupDate} className="bg-white rounded-xl shadow-md overflow-hidden border border-gray-200">
                                                <div className="bg-yellow-300 p-3">
                                                    <h3 className="font-bold text-yellow-800">
                                                        {new Date(dateGroup.groupDate).toLocaleDateString('en-US', { month: 'long', day: '2-digit', year: 'numeric' })}
                                                        <span className="font-normal ml-2">({dateGroup.itemCount} items)</span>
                                                    </h3>
                                                </div>
                                                <div className="overflow-x-auto">
                                                    <table className="min-w-full text-sm">
                                                        <thead className="bg-gray-50">
                                                            <tr>
                                                                <th className="p-3 text-left font-semibold text-gray-600 w-12">#</th>
                                                                <th className="p-3 text-left font-semibold text-gray-600 w-24">Image</th>
                                                                <th className="p-3 text-left font-semibold text-gray-600">Name</th>
                                                                <th className="p-3 text-left font-semibold text-gray-600">Note</th>
                                                                <th className="p-3 text-center font-semibold text-gray-600" colSpan="3">Documents</th>
                                                                <th className="p-3 text-left font-semibold text-gray-600">Action</th>
                                                            </tr>
                                                        </thead>
                                                        <tbody className="divide-y divide-gray-200">
                                                            {dateGroup.details.map((detail, index) => (
                                                                <tr key={detail.planDetailId} className="hover:bg-gray-50">
                                                                    <td className="p-3 text-center text-gray-500">{index + 1}</td>
                                                                    <td className="p-3">
                                                                        <img src={detail.imageUrl || 'https://placehold.co/100x100/e2e8f0/adb5bd?text=N/A'} alt={detail.customerName} className="w-16 h-16 object-cover rounded-md border" />
                                                                    </td>
                                                                    <td className="p-3">
                                                                        <div className="font-bold text-gray-800">{detail.customerName}</div>
                                                                        <div className="text-xs text-gray-500">{detail.orderCode}</div>
                                                                    </td>
                                                                    <td className="p-3 text-gray-600 max-w-xs truncate">{detail.noteOrEngravingContent || 'N/A'}</td>
                                                                    <td className="p-3 text-center">
                                                                        <a href={detail.productionFileUrl} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-2 text-blue-600 hover:text-blue-800 hover:underline disabled:opacity-50">
                                                                            <DownloadIcon /> Download File
                                                                        </a>
                                                                    </td>
                                                                    <td className="p-3 text-center">
                                                                        <a href={detail.thankYouCardUrl} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-2 text-green-600 hover:text-green-800 hover:underline">
                                                                            <CardIcon /> Download Card
                                                                        </a>
                                                                    </td>
                                                                    <td className="p-3 text-center">
                                                                        <a href="#" className="inline-flex items-center gap-2 text-purple-600 hover:text-purple-800 hover:underline">
                                                                            <LabelIcon /> Print Label
                                                                        </a>
                                                                    </td>
                                                                    <td className="p-3">
                                                                        <button
                                                                            className="bg-blue-500 text-white px-4 py-2 rounded-md hover:bg-blue-600 transition">
                                                                            Update Status
                                                                        </button>
                                                                    </td>
                                                                </tr>
                                                            ))}
                                                        </tbody>
                                                    </table>
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            ))
                        )}
                    </div>
                </div>
            </main>
        </div>
    );
}