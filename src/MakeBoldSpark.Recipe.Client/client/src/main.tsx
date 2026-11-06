import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import './brand.css';
import './base.css';
import './styles.css';
createRoot(document.getElementById('root')!).render(<StrictMode><App /></StrictMode>);
