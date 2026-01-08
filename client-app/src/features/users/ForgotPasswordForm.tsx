import { ErrorMessage, Form as FormikForm, Formik } from "formik";
import { observer } from "mobx-react-lite";
import { Button, Form, Label, Message } from "semantic-ui-react";
import { useStore } from "../../app/stores/store";
import * as Yup from 'yup';
import { useState } from "react";
import { Link } from "react-router-dom";

export default observer(function ForgotPasswordForm() {
    const { userStore } = useStore();
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    const validationSchema = Yup.object({
        email: Yup.string().required('Sähköposti vaaditaan').email('Virheellinen sähköpostiosoite')
    });

    return (
        <div className="login-page">
            <div className="login-card">
                <div className="login-logo">M</div>
                <h1 className="login-title">Unohtuiko salasana?</h1>

                <Formik
                    initialValues={{ email: '', error: null }}
                    validationSchema={validationSchema}
                    onSubmit={(values, { setErrors, setSubmitting }) => userStore.forgotPassword({
                        email: values.email
                    }).then((message) => {
                        setSuccessMessage(message);
                        setSubmitting(false);
                    }).catch(error => {
                        setErrors({ error: 'Salasanan palautusviestin lähetys epäonnistui.' });
                        setSubmitting(false);
                    })}
                >
                    {({ handleSubmit, isSubmitting, errors, handleChange, values, touched }) => (
                        <FormikForm className='ui form login-form' onSubmit={handleSubmit} autoComplete='off'>
                            {successMessage ? (
                                <Message positive>
                                    <Message.Header>Sähköposti lähetetty</Message.Header>
                                    <p>{successMessage}</p>
                                    <p>Tarkista sähköpostisi ja seuraa ohjeita salasanan nollaamiseksi.</p>
                                    <p><Link to='/login'>Palaa kirjautumiseen</Link></p>
                                </Message>
                            ) : (
                                <>
                                    <p style={{ textAlign: 'center', marginBottom: 20, color: 'var(--text-secondary)' }}>
                                        Syötä sähköpostiosoitteesi ja lähetämme sinulle linkin salasanan nollaamiseksi.
                                    </p>

                                    <Form.Input
                                        name='email'
                                        placeholder='Sähköposti'
                                        type='email'
                                        value={values.email}
                                        onChange={handleChange}
                                        icon='mail'
                                        iconPosition='left'
                                        error={touched.email && errors.email}
                                        fluid
                                    />
                                    <ErrorMessage name='email' render={error => <Label style={{ marginBottom: 10 }} basic color='red' content={error} />} />

                                    <ErrorMessage
                                        name='error' render={() =>
                                            <Label style={{ marginBottom: 10, width: '100%', textAlign: 'center' }} basic color='red' content={errors.error} />}
                                    />
                                    <Button
                                        loading={isSubmitting}
                                        className="btn-primary"
                                        content='Lähetä palautuslinkki'
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
