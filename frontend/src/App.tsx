import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Navbar from './components/Navbar/Navbar';
import AddTransaction from './pages/AddTransaction';
import Monitor from './pages/Monitor';
import './App.css';

function App() {
  return (
    <BrowserRouter>
      <Navbar />
      <main>
        <Routes>
          <Route path="/add" element={<AddTransaction />} />
          <Route path="/monitor" element={<Monitor />} />
          <Route path="*" element={<Navigate to="/monitor" replace />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}

export default App;
