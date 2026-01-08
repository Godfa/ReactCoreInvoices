import { ErrorMessage, Form as FormikForm, Formik } from "formik";
import { observer } from "mobx-react-lite";
import { Button, Form, Label, Message } from "semantic-ui-react";
import { useStore } from "../../app/stores/store";
import * as Yup from 'yup';
import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";

export default observer(function ResetPasswordForm() {
    const { userStore } = useStore();
    const [searchParams] = useSearchParams();
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    const email = searchParams.get('email') || '';
    const token = searchParams.get('token') || '';

    const validationSchema = Yup.object({
        newPassword: Yup.string().required('Uusi salasana vaaditaan').min(12, 'Salasanan on oltava vähintään 12 merkkiä'),
        confirmPassword: Yup.string()
            .required('Vahvista salasana')
            .oneOf([Yup.ref('newPassword')], 'Salasanojen on täsmättävä')
    });

    if (!email || !token) {
        return (
            <div className="login-page">
                <div className="login-card">
                    <div className="login-logo">M</div>
                    <h1 className="login-title">Nollaa salasana</h1>
                    <Message negative>
                        <Message.Header>Virheellinen palautuslinkki</Message.Header>
                        <p>Tämä salasanan palautuslinkki on virheellinen tai vanhentunut.</p>
                        <p><Link to='/forgot-password'>Pyydä uusi palautuslinkki</Link></p>
                    </Message>
                </div>
            </div>
        );
    }

    return (
        <div className="login-page">
            <div className="login-card">
                <div className="login-logo">M</div>
                <h1 className="login-title">Nollaa salasana</h1>

                <Formik
                    initialValues={{ newPassword: '', confirmPassword: '', error: null }}
                    validationSchema={validationSchema}
                    onSubmit={(values, { setErrors, setSubmitting }) => userStore.resetPassword({
                        email,
                        token,
                        newPassword: values.newPassword
                    }).then((message) => {
                        setSuccessMessage(message);
                        setSubmitting(false);
                    }).catch(error => {
                        setErrors({ error: 'Salasanan nollaus epäonnistui. Linkki saattaa olla vanhentunut.' });
                        setSubmitting(false);
                    })}
                >
                    {({ handleSubmit, isSubmitting, errors, handleChange, values, touched }) => (
                        <FormikForm className='ui form login-form' onSubmit={handleSubmit} autoComplete='off'>
                            {successMessage ? (
                                <Message positive>
                                    <Message.Header>Salasana nollattu onnistuneesti</Message.Header>
                                    <p>{successMessage}</p>
                                    <p>Voit nyt kirjautua sisään uudella salasanallasi.</p>
                                    <p><Link to='/login'>Siirry kirjautumiseen</Link></p>
                                </Message>
                            ) : (
                                <>
                                    <p style={{ textAlign: 'center', marginBottom: 20, color: 'var(--text-secondary)' }}>
                                        Syötä uusi salasanasi alle.
                                    </p>

                                    <Form.Input
                                        name='newPassword'
                                        placeholder='Uusi salasana'
                                        type='password'
                                        value={values.newPassword}
                                        onChange={handleChange}
                                        icon='lock'
                                        iconPosition='left'
                                        error={touched.newPassword && errors.newPassword}
                                        fluid
                                    />
                                    <ErrorMessage name='newPassword' render={error => <Label style={{ marginBottom: 10 }} basic color='red' content={error} />} />

                                    <Form.Input
                                        name='confirmPassword'
                                        placeholder='Vahvista uusi salasana'
                                        type='password'
                                        value={values.confirmPassword}
                                        onChange={handleChange}
                                        icon='lock'
                                        iconPosition='left'
                                        error={touched.confirmPassword && errors.confirmPassword}
                                        fluid
                                    />
                                    <ErrorMessage name='confirmPassword' render={error => <Label style={{ marginBottom: 10 }} basic color='red' content={error} />} />

                                    <ErrorMessage
                                        name='error' render={() =>
                                            <Label style={{ marginBottom: 10, width: '100%', textAlign: 'center' }} basic color='red' content={errors.error} />}
                                    />
                                    <Button
                                        loading={isSubmitting}
                                        className="btn-primary"
                                        content='Nollaa salasana'
                                        type='submit'
                                        fluid
                                        size="large"
                                    />

                                    <div className="login-footer">
                                        <Link to='/login'>Takaisin kirjautumiseen</Link>
                                    </div>
                                </>
                            )}
                        </FormikForm>
                    )}
                </Formik>
            </div>
        </div>
    )
})
