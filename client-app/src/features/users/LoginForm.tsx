import { ErrorMessage, Form as FormikForm, Formik } from "formik";
import { observer } from "mobx-react-lite";
import { Button, Form, Label } from "semantic-ui-react";
import { useStore } from "../../app/stores/store";
import { Link } from "react-router-dom";

export default observer(function LoginForm() {
    const { userStore } = useStore();

    return (
        <div className="login-page">
            <div className="login-card">
                <div className="login-logo">M</div>
                <h1 className="login-title">Mökkilan Invoices</h1>

                <Formik
                    initialValues={{ email: '', password: '', error: null }}
                    onSubmit={(values, { setErrors }) => userStore.login(values).catch(error => {
                        const message = typeof error.response?.data === 'string'
                            ? error.response.data
                            : 'Virheellinen sähköposti, käyttäjätunnus tai salasana';
                        setErrors({ error: message });
                    })}
                >
                    {({ handleSubmit, isSubmitting, errors, handleChange, values }) => (
                        <FormikForm className='ui form login-form' onSubmit={handleSubmit} autoComplete='off'>
                            <Form.Input
                                name='email'
                                placeholder='Sähköposti tai käyttäjätunnus'
                                value={values.email}
                                onChange={handleChange}
                                icon='user'
                                iconPosition='left'
                                fluid
                            />
                            <Form.Input
                                name='password'
                                placeholder='Salasana'
                                type='password'
                                value={values.password}
                                onChange={handleChange}
                                icon='lock'
                                iconPosition='left'
                                fluid
                            />
                            <ErrorMessage
                                name='error' render={() =>
                                    <Label style={{ marginBottom: 10, width: '100%', textAlign: 'center' }} basic color='red' content={errors.error} />}
                            />
                            <Button
                                loading={isSubmitting}
                                className="btn-primary"
                                content='Kirjaudu sisään'
                                type='submit'
                                fluid
                                size="large"
                            />
                        </FormikForm>
                    )}
                </Formik>

                <div className="login-footer">
                    <Link to='/forgot-password'>
                        Unohtuiko salasana?
                    </Link>
                </div>
            </div>
        </div>
    )
})
