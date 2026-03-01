import { NavLink } from 'react-router-dom';
import styles from './Navbar.module.css';

export default function Navbar() {
  return (
    <nav className={styles.navbar}>
      <span className={styles.brand}>💰 Financial Monitor</span>
      <div className={styles.links}>
        <NavLink
          to="/add"
          className={({ isActive }) => isActive ? styles.active : styles.link}
        >
          Simulator
        </NavLink>
        <NavLink
          to="/monitor"
          className={({ isActive }) => isActive ? styles.active : styles.link}
        >
          Live Dashboard
        </NavLink>
      </div>
    </nav>
  );
}
