import React, { useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { Dropdown, Icon } from 'semantic-ui-react';
import { observer } from 'mobx-react-lite';
import { useStore } from '../stores/store';

interface Props {
    mobileOpen?: boolean;
    onMobileClose?: () => void;
}

export default observer(function Sidebar({ mobileOpen, onMobileClose }: Props) {
    const { userStore } = useStore();
    const location = useLocation();
    const [userMenuOpen, setUserMenuOpen] = useState(false);

    const isActive = (path: string) => {
        if (path === '/invoices') {
            return location.pathname === '/invoices' || location.pathname === '/';
        }
        return location.pathname.startsWith(path);
    };

    const getInitials = (name?: string) => {
        if (!name) return 'U';
        return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
    };

    const handleNavClick = () => {
        if (onMobileClose) {
            onMobileClose();
        }
    };

    if (!userStore.isLoggedIn) return null;

    return (
        <>
            {/* Mobile overlay */}
            {mobileOpen && (
                <div
                    className="sidebar-overlay"
                    onClick={onMobileClose}
                    style={{
                        position: 'fixed',
                        top: 0,
                        left: 0,
                        right: 0,
                        bottom: 0,
                        background: 'rgba(0,0,0,0.5)',
                        zIndex: 999
                    }}
                />
            )}

            <aside className={`sidebar ${mobileOpen ? 'mobile-open' : ''}`}>
                {/* Header */}
                <div className="sidebar-header">
                    <div className="sidebar-logo">M</div>
                    <span className="sidebar-title">Mökkilan Invoices</span>
                </div>

                {/* Navigation */}
                <nav className="sidebar-nav">
                    <NavLink
                        to="/invoices"
                        className={`sidebar-nav-item ${isActive('/invoices') ? 'active' : ''}`}
                        onClick={handleNavClick}
                    >
                        <Icon name="dashboard" />
                        <span>Ohjauspaneeli</span>
                    </NavLink>

                    <NavLink
                        to="/createInvoice"
                        className={`sidebar-nav-item ${isActive('/createInvoice') ? 'active' : ''}`}
                        onClick={handleNavClick}
                    >
                        <Icon name="plus" />
                        <span>Luo lasku</span>
                    </NavLink>

                    {userStore.user?.roles?.includes('Admin') && (
                        <NavLink
                            to="/admin"
                            className={`sidebar-nav-item ${isActive('/admin') ? 'active' : ''}`}
                            onClick={handleNavClick}
                        >
                            <Icon name="settings" />
                            <span>Järjestelmänvalvoja</span>
                        </NavLink>
                    )}
                </nav>

                {/* Footer with user */}
                <div className="sidebar-footer">
                    <Dropdown
                        open={userMenuOpen}
                        onOpen={() => setUserMenuOpen(true)}
                        onClose={() => setUserMenuOpen(false)}
                        trigger={
                            <div className="sidebar-user">
                                <div className="sidebar-user-avatar">
                                    {getInitials(userStore.user?.displayName)}
                                </div>
                                <div className="sidebar-user-info">
                                    <div className="sidebar-user-name">{userStore.user?.displayName}</div>
                                </div>
                                <Icon name="chevron down" style={{ marginLeft: 'auto', opacity: 0.5 }} />
                            </div>
                        }
                        icon={null}
                        direction="left"
                        pointing="bottom left"
                    >
                        <Dropdown.Menu>
                            <Dropdown.Item
                                as={NavLink}
                                to="/changePassword"
                                icon="key"
                                text="Vaihda salasana"
                                onClick={handleNavClick}
                            />
                            <Dropdown.Divider />
                            <Dropdown.Item
                                icon="sign out"
                                text="Kirjaudu ulos"
                                onClick={() => {
                                    userStore.logout();
                                    handleNavClick();
                                }}
                            />
                        </Dropdown.Menu>
                    </Dropdown>
                </div>
            </aside>
        </>
    );
});
