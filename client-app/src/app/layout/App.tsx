import React, { useEffect } from 'react';
import { Container } from 'semantic-ui-react';
import NavBar from './NavBar';
import { useStore } from '../stores/store';
import { observer } from 'mobx-react-lite';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';

function App() {

  const { userStore } = useStore();
  const location = useLocation();
  const navigate = useNavigate();

  useEffect(() => {
    if (userStore.user) {
      // User already logged in
    } else {
      const token = window.localStorage.getItem('jwt');
      if (token) {
        userStore.getUser();
      }
    }
  }, [userStore])

  useEffect(() => {
    if (userStore.user?.mustChangePassword && location.pathname !== '/changePassword') {
      navigate('/changePassword');
    }
  }, [userStore.user, location.pathname, navigate])

  return (
    <>
      <NavBar />
      <Container style={{ marginTop: '7em' }}>
        <Outlet />
      </Container>
    </>
  );
}

export default observer(App);
